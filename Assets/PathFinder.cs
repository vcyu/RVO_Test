using System.Collections.Generic;
using UnityEngine;

public class PathFinder  {
    
	private float m_cellsize = 1.0f;
    private float m_minX;
	private float m_maxX;
    private float m_minY;
    private float m_maxY;

	private Cell[,] m_cells;
    private List<Rect> m_blocks;
	private List<Cell> m_openCells;
	private int m_cellCountX;
	private int m_cellCountY;
	private Cell m_startCell;
	private Cell m_targetCell;

    class Cell
    {
        public Cell(bool isBlock, int numX, int numY, Vector2 position)
        {
            m_position = position;
            m_isBlock = isBlock;
            m_isClosed = isBlock;
            m_cellNumX = numX;
            m_cellNumY = numY;
            m_valueF = -1;
            m_valueG = -1;
            m_valueH = -1;
            m_parentCell = null;
        }

        public Vector2 m_position;
        public bool m_isBlock;
        public bool m_isClosed;
        public int m_cellNumX;
        public int m_cellNumY;
        public int m_valueF;
        public int m_valueG;
        public int m_valueH;
        public Cell m_parentCell;
    }

    public PathFinder(float minX, float maxX, float minY, float maxY, float cell_size)
    {
        m_minX = minX;
        m_maxX = maxX;
        m_minY = minY;
        m_maxY = maxY;
		m_cellsize = cell_size;
		List<Rect> blocks = new List<Rect>();
    }

	public void AddBlock(Rect block)
	{

		m_blocks.Add(new Rect(-5, -5, 10, 10));
	}

	public void InitCells()
	{
		// 计算格子数量，创建格子
		m_cellCountX = (int)(m_maxX - m_minX / m_cellsize);
		m_cellCountY = (int)(m_maxY - m_minY / m_cellsize);
		m_cells = new Cell[m_cellCountX, m_cellCountY];

		// 初始化格子
		int numX = 0;
		int numY = 0;
		for (float pointX = m_minX; pointX <= m_maxX - m_cellsize; pointX += m_cellsize)
		{
			for (float pointY = m_minY; pointY <= m_maxY - m_cellsize; pointY += m_cellsize)
			{
				bool isBlocked = false;
				Rect cell = new Rect(pointX, pointY, m_cellsize, m_cellsize);
				foreach (Rect block in m_blocks)
				{
					if (CellIsBlocked(cell, block))
					{
						isBlocked = true;
					}
				}
				m_cells[numX, numY] = new Cell(isBlocked, numX, numY, new Vector2(pointX + m_cellsize / 2, pointY + m_cellsize / 2));

				numY++;
			}
		}
	}

	public List<Vector2> FindPath(Vector2 startPos, Vector2 targetPos)
	{
		// 先找到起点和终点对应的cell
		m_startCell = FindCellByPos(startPos);
		m_targetCell = FindCellByPos(targetPos);
		List<Vector2> wayCells = new List<Vector2>();

		if (m_startCell == m_targetCell)
		{
			// 如果起点终点在同一格子
			return wayCells;
		}
		else
		{
			SetCellValues(m_startCell, null);
			// 向开放列表内加入起始cell
			m_openCells.Add(m_startCell);
			while (m_openCells.Count > 0)
			{
				m_openCells.Sort(delegate (Cell x, Cell y)
				{
					return x.m_valueF - y.m_valueF;
				});

				if (SearchAndCloseCell(m_openCells[0]) == true)
				{
					break;
				}
			}

			if (m_targetCell.m_parentCell == null)
			{
                // 最终没找到路径
				return null;
			}

            // 生成路径点序列
			Cell curCell = m_targetCell.m_parentCell;
			while (curCell != null && curCell != m_startCell)
			{
                wayCells.Insert(0, curCell.m_position);
				curCell = curCell.m_parentCell;
			}

            // 重置所有cell，以待下次使用
            ResetCells();

			return wayCells;
		}
	}

	private bool CellIsBlocked(Rect cell, Rect block)
	{
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 2; j++)
			{
                // 查找cell四角，是否落在block里面
				Vector2 point =
					new Vector2(i == 0 ? cell.xMin : cell.xMax, j == 0 ? cell.yMin : cell.yMax);
				if (block.Contains(point))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ResetCells()
	{
		foreach (Cell cell in m_cells)
		{
            cell.m_isClosed = cell.m_isBlock;
			cell.m_valueF = -1;
			cell.m_valueG = -1;
			cell.m_valueH = -1;
			cell.m_parentCell = null;
		}
	}

	private Cell FindCellByPos(Vector2 pos)
	{
		int min = 0;
		int max = m_cellCountX - 1;
		int curX = max / 2;
		int curY = 0;

		while (pos.x > m_cells[curX, curY].m_position.x + m_cellsize / 2 || pos.x < m_cells[curX, curY].m_position.x - m_cellsize / 2)
		{
			if (pos.x > m_cells[curX, curY].m_position.x + m_cellsize / 2)
			{
				min = curX + 1;
			}
			else
			{
				max = curX - 1;
			}
			curX = (min + max) / 2;
		}

		min = 0;
		max = m_cellCountY - 1;
		curY = max / 2;
		while (pos.y > m_cells[curX, curY].m_position.y + m_cellsize / 2 || pos.y < m_cells[curX, curY].m_position.y - m_cellsize / 2)
		{
			if (pos.y > m_cells[curX, curY].m_position.y + m_cellsize / 2)
			{
				min = curX + 1;
			}
			else
			{
				max = curX - 1;
			}
			curX = (min + max) / 2;
		}

		return m_cells[curX, curY];
	}

	private void SetCellValues(Cell curCell, Cell parentCell)
	{
		bool isChanged = false;
		if (parentCell == null)
		{
			// 没有父节点，则必是第一个节点，故G为0，F无需计算
			curCell.m_valueG = 0;
			curCell.m_valueF = 0;
		}
		else
		{
			// 计算与父节点的距离
			int valueG = parentCell.m_valueG;
			if (Vector2.Distance(curCell.m_position, parentCell.m_position) > 1.1 * m_cellsize)
			{
				// 斜线
				valueG += 14;
			}
			else
			{
				// 直线
				valueG += 10;
			}

			// 如果比现有距离更短，则使用这个更近的父节点
			if (curCell.m_valueG == -1 || valueG < curCell.m_valueG)
			{
				curCell.m_valueG = valueG;
				curCell.m_parentCell = parentCell;
				isChanged = true;
			}
		}

		// 计算到目标的直线距离H，只计算一次，不会变化
		if (curCell.m_valueH == -1)
		{
			curCell.m_valueH = Mathf.RoundToInt(
				(Mathf.Abs(m_targetCell.m_position.x - curCell.m_position.x) +
				 Mathf.Abs(m_targetCell.m_position.y - curCell.m_position.y)) / m_cellsize);
		}

		// 如果F值未设定，或G值出现变化，则更新F值
		if (isChanged || curCell.m_valueF < 0)
		{
			curCell.m_valueF = curCell.m_valueG + curCell.m_valueH * 10;
		}
	}

	private bool SearchAndCloseCell(Cell sourceCell)
	{
		// 将源cell移出open列表，并设置为closed
		sourceCell.m_isClosed = true;
        m_openCells.Remove(sourceCell);

		// 遍历周边所有cell
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				int cellNumX = sourceCell.m_cellNumX + x;
				int cellNumY = sourceCell.m_cellNumY + y;


				if (cellNumX < 0 || cellNumY < 0 || cellNumX >= m_cellCountX || cellNumY >= m_cellCountY)
				{
					// 不存在的cell
					continue;
				}

				Cell cell = m_cells[cellNumX, cellNumY];

				if (cell.m_isBlock && cell.m_isClosed)
				{
					// cell被阻碍或者已关闭
					continue;
				}


				if (x != 0 && y != 0 &&
                    (m_cells[cellNumX - x, cellNumY].m_isBlock || m_cells[cellNumX, cellNumY - y].m_isBlock))
				{
					// 斜向通路上有阻碍
					continue;
				}

				// 计算cell的通路值，并加入open列表
				SetCellValues(cell, sourceCell);
				if (!m_openCells.Contains(cell))
				{
					m_openCells.Add(cell);
				}

				if (cell == m_targetCell)
				{
					// 如果已抵达目标cell
					return true;
				}
			}
		}

		return false;
	}
}
