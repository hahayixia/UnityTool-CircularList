using System;
using System.Collections.Generic;
using UnityEngine;

public class CircularList : MonoBehaviour
{
    public struct IntVector2
    {
        public int x;
        public int y;
        public IntVector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
    public enum RankType
    {
        Horizontal,
        Vertical
    }
    [SerializeField]
    private Vector2 _cellSize;
    [SerializeField]
    private RankType _rankType = RankType.Horizontal;
    [SerializeField]
    private bool _loopx;
    [SerializeField]
    private bool _loopy;
    [SerializeField]
    private RectTransform _container;
    //0,1,2
    //3,4,5
    //6,7,8
    //单元UI横向排序
    private List<RectTransform> _items;
    //cellindex,dataindex,localPos
    private Action<int, int, Vector3> _onRender;

    public Action<int, Vector3> _onPosChange;

    public float ItemWight
    {
        get
        {
            return _cellSize.x;
        }
    }
    public float ItemHeight
    {
        get
        {
            return _cellSize.y;
        }
    }
    private int data_xcount;
    private int data_ycount;
    private int cell_xcount;
    private int cell_ycount;

    int[] recordArr = null;
    int[] recordPosArr = null;
    Vector2 CurPos
    {
        get
        {
            return _container.anchoredPosition;
        }
        set
        {
            _container.anchoredPosition = value;
        }
    }
    bool havedata = false;
    public void InitData(int cellcountX, int cellcountY, Action<int, int, Vector3> onRender)
    {
        _onRender = onRender;

        _container = _container == null ? transform as RectTransform : _container;
        Vector2 targetpivot = new Vector2(0, 1);
        Vector2 dpivot = targetpivot - _container.pivot;
        _container.pivot = targetpivot;
        _container.anchoredPosition += new Vector2(dpivot.x * _container.sizeDelta.x, dpivot.y * _container.sizeDelta.y);
        _container.anchorMin = targetpivot;
        _container.anchorMin = targetpivot;

        cell_xcount = cellcountX;
        cell_ycount = cellcountY;
        recordArr = new int[cell_xcount * cell_ycount];
        recordPosArr = new int[cell_xcount * cell_ycount];
        for (int i = 0; i < recordArr.Length; i++)
        {
            recordArr[i] = -1;
        }
    }
    public void RefreshData(int x, int y, bool nosizedelta = false)
    {
        havedata = true;
        if (!nosizedelta) _container.sizeDelta = new Vector2(x * _cellSize.x, y * _cellSize.y);
        data_xcount = x;
        data_ycount = y;
        UpdateRender(true);
    }
    Vector2 oldPos = new Vector2(float.MinValue, float.MinValue);
    public void UpdateRender(bool force = false)
    {
        if (_container == null) return;
        if (cell_xcount * cell_ycount == 0) return;
        if (!force)
        {
            Vector2 dvalue = CurPos - oldPos;
            if (Mathf.Abs(dvalue.x) < _cellSize.x / 4 && Mathf.Abs(dvalue.y) < _cellSize.y / 4) return;
        }
        oldPos = CurPos;
        IntVector2 topleft = FirstDataPos();
        IntVector2 bottomright = new IntVector2(topleft.x + cell_xcount, topleft.y + cell_ycount);
        for (int i = topleft.x; i < bottomright.x; i++)
        {
            for (int j = topleft.y; j < bottomright.y; j++)
            {
                IntVector2 v2 = PosV2ToDataV2(i, j);

                if (v2.x < 0) continue;
                if (v2.y < 0) continue;

                int di = DataIndex(v2.x, v2.y);
                int ci = CellIndex(i, j);

                if (CheckChangeAndRecord(ci, di) || force)
                {
                    _onRender(ci, di, new Vector3(i * _cellSize.x, -j * _cellSize.y, 0));
                }
                else if ((_loopx || _loopy) && CheckPosChangeAndRecord(ci, i, j))
                {
                    if (_onPosChange != null) _onPosChange(ci, new Vector3(i * _cellSize.x, -j * _cellSize.y, 0));
                }
            }
        }
    }
    public void GoTo(int x, int y)
    {
        CurPos = GetPos(x, y);
    }
    public Vector2Int GetNearstPos()
    {
        Vector2Int re = new Vector2Int(Mathf.RoundToInt(CurPos.x / _cellSize.x), Mathf.RoundToInt(CurPos.y / _cellSize.y));
        return re;
    }
    IntVector2 FirstDataPos()
    {
        IntVector2 re = new IntVector2(Mathf.FloorToInt(-CurPos.x / _cellSize.x), Mathf.FloorToInt(CurPos.y / _cellSize.y));
        if (_loopx)
        {
            re.x -= 1;
        }
        else if (re.x < 0)
        {
            re.x = 0;
        }
        if (_loopy)
        {
            re.y -= 1;
        }
        else if (re.y < 0)
        {
            re.y = 0;
        }
        return re;
    }
    IntVector2 PosV2ToDataV2(int x, int y)
    {
        int m = x;
        int n = y;
        if (_loopx)
        {
            if (data_xcount > 0)
            {
                m = m % data_xcount;
                if (m < 0)
                {
                    m += data_xcount;
                }
            }
        }
        if (_loopy)
        {
            if (data_ycount > 0)
            {
                n = n % data_ycount;
                if (n < 0)
                {
                    n += data_ycount;
                }
            }
        }
        return new IntVector2(m, n);
    }
    int DataIndex(int x, int y)
    {
        switch (_rankType)
        {
            case RankType.Horizontal:
                return y * data_xcount + x;
            case RankType.Vertical:
                return x * data_ycount + y;
        }
        return -1;
    }
    int CellIndex(int x, int y)
    {
        x = x % cell_xcount;
        if (x < 0)
        {
            x += cell_xcount;
        }
        y = y % cell_ycount;
        if (y < 0)
        {
            y += cell_ycount;
        }
        return y % cell_ycount * cell_xcount + x % cell_xcount;
    }
    Vector2 GetPos(int x, int y)
    {
        return new Vector2(_cellSize.x * x, _cellSize.y * y);
    }
    bool CheckChangeAndRecord(int cellIndex, int dataIndex)
    {
        int old = recordArr[cellIndex];
        if (old != dataIndex)
        {
            recordArr[cellIndex] = dataIndex;
            return true;
        }
        return false;
    }
    bool CheckPosChangeAndRecord(int cellIndex, int x, int y)
    {
        int old = recordPosArr[cellIndex];
        int v = x * 1000000 + y;
        if (old != v)
        {
            recordPosArr[cellIndex] = v;
            return true;
        }
        return false;
    }
    private void Update()
    {
        if (havedata)
            UpdateRender();
    }
}
