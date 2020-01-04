using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Properties")]
    public int rows;
    public int columns;
    public float lineWidth;
    public Vector2 cellDims;
    [Header("Info")]
    public GridInfo gridInfo;
    public List<GridLineInfo> horizontalLines;
    public List<GridLineInfo> verticalLines;
    public List<SectionInfo> sections;
    private List<GameObject> shelfLineObjs;
    [Header("Prefabs")]
    public GameObject shelfLinePrefab;
    public GameObject shelfWallPrefab;
    [Header("Controls")]
    public bool respawnGrid;

    private void Awake() {
        horizontalLines = new List<GridLineInfo>();
        verticalLines = new List<GridLineInfo>();
        shelfLineObjs = new List<GameObject>();
    }

    // Start is called before the first frame update
    void Start()
    {
        SpawnGrid();

        GenerateBaseSection();

        // Spawn lines
        //Debug_SpawnFullGridLines();

        // Spawn shelf wallpaper
        GameObject shelfWallpaper = Instantiate(shelfWallPrefab, gridInfo.gridOrigin, Quaternion.identity);
        shelfWallpaper.transform.localScale = gridInfo.GridSize();

        BuildRandomCabinet();
        StartCoroutine(Debug_SpawnRandomShelves());
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator Debug_SpawnRandomShelves()
    {
        yield return new WaitForSeconds(0.5f);

        BuildRandomCabinet();

        StartCoroutine(Debug_SpawnRandomShelves());
    }

    void BuildRandomCabinet()
    {
        ClearShelfLines();
        int divideAmt = Random.Range((columns + rows) / 2, (int) ((columns + rows) * 1.5f));
        while (divideAmt-- > 0)
        {
            RandomSubdivide();
        }   
    }

    void GenerateBaseSection()
    {
        sections = new List<SectionInfo>();

        SectionInfo baseSection = new SectionInfo();
        for (int x = 0; x < columns; ++x)
        {
            baseSection.sectionGrid.Add(new ColumnSectionInfo());
            for (int  y = 0; y < rows; ++y)
            {
                CellInfo newCell = new CellInfo();
                newCell.origin = new Vector2(gridInfo.gridVerticalLimits.x, gridInfo.gridHorizontalLimits.x) + new Vector2(x, y) * (cellDims + new Vector2(lineWidth, lineWidth)) + cellDims / 2.0f;
                baseSection.sectionGrid[x].columnSection.Add(newCell);
            }
        }

        sections.Add(baseSection);
    }

    void SpawnGrid()
    {
        // Calculate grid info
        //gridInfo.gridOrigin = new Vector2(rows % 2 ?  : 0)
        float horizontalHalfSize = (rows * cellDims.y + (rows - 1) * lineWidth) / 2;
        float verticalHalfSize = (columns * cellDims.x + (columns - 1) * lineWidth) / 2;
        gridInfo.gridHorizontalLimits = new Vector2(-horizontalHalfSize, horizontalHalfSize);
        gridInfo.gridVerticalLimits = new Vector2(-verticalHalfSize, verticalHalfSize);
    }

    void Debug_SpawnFullGridLines()
    {
        // Calculate grid line info
        for (int i = 1; i < rows; ++i)
        {
            GridLineInfo currentLine = new GridLineInfo();
            currentLine.lineOffset = gridInfo.gridHorizontalLimits.x + i * cellDims.y + (i - 1) * lineWidth + lineWidth / 2.0f;
            horizontalLines.Add(currentLine);
        }
        for (int i = 1; i < columns; ++i)
        {
            GridLineInfo currentLine = new GridLineInfo();
            currentLine.lineOffset = gridInfo.gridVerticalLimits.x + i * cellDims.x + (i - 1) * lineWidth + lineWidth / 2.0f;
            verticalLines.Add(currentLine);
        }

        for (int i = 0; i < horizontalLines.Count; ++i)
        {
            SpawnShelfLine(gridInfo.gridOrigin + new Vector2(0, horizontalLines[i].lineOffset), new Vector3(gridInfo.GridSize().x, lineWidth, 1));
        }
        for (int i = 0; i < verticalLines.Count; ++i)
        {
            SpawnShelfLine(gridInfo.gridOrigin + new Vector2(verticalLines[i].lineOffset, 0), new Vector3(lineWidth, gridInfo.GridSize().y, 1));
        }
    }

    bool DirectionalRandomSubdivide(bool isHorizontal)
    {
        // Select random section
        int sectionIndex = Random.Range(0, sections.Count);
        bool canSubdivide = false;

        for (int i = 0; i < sections.Count; ++i, ++sectionIndex)
        {
            if (sectionIndex == sections.Count) sectionIndex = 0;

            // Check if can be sub-divided in chosen direction
            canSubdivide = sections[sectionIndex].CanSubdivide(isHorizontal);
            if (canSubdivide) break;
        }
        
        // No sections can be subdivided
        if (canSubdivide == false) return false;

        // Subdivide
        SingleSubdivide(isHorizontal, sections[sectionIndex]);

        return true;
    }

    bool SingleSubdivide(bool isHorizontal, SectionInfo divdedSection)
    {
        int subdivideDim = (int) (isHorizontal ? divdedSection.Dimensions().y : divdedSection.Dimensions().x);
        int adjDim = (int) (isHorizontal ? divdedSection.Dimensions().x : divdedSection.Dimensions().y);
        int subdividePos = Mathf.CeilToInt((subdivideDim / 2.0f) - 1.0f);
        //int subdividePos = Random.Range(0, subdivideDim - 1);
        if (subdivideDim == 1) return false;

        // Split section
        SectionInfo firstSection = new SectionInfo();
        SectionInfo secondSection = new SectionInfo();
        if (isHorizontal)
        {
            for (int x = 0; x < divdedSection.Dimensions().x; ++x)
            {
                firstSection.sectionGrid.Add(new ColumnSectionInfo());
                secondSection.sectionGrid.Add(new ColumnSectionInfo());
                for (int y = 0; y < divdedSection.Dimensions().y; ++y)
                {
                    if (y <= subdividePos) firstSection.sectionGrid[x].columnSection.Add(divdedSection.sectionGrid[x].columnSection[y]);
                    else secondSection.sectionGrid[x].columnSection.Add(divdedSection.sectionGrid[x].columnSection[y]);
                }
            }
        }
        else
        {
            for (int x = 0; x < divdedSection.Dimensions().x; ++x)
            {
                if (x <= subdividePos) firstSection.sectionGrid.Add(divdedSection.sectionGrid[x]);
                else secondSection.sectionGrid.Add(divdedSection.sectionGrid[x]);
            }
        }

        // Add new sections
        sections.Add(firstSection);
        sections.Add(secondSection);

        // Remove dividedSection from sections
        sections.Remove(divdedSection);

        // Spawn grid line
        Vector2 lineOrigin = new Vector2();
        Vector3 lineScale = new Vector3();
        if (isHorizontal)
        {
            int back = divdedSection.sectionGrid.Count - 1;
            float cellWidth = divdedSection.sectionGrid[back].columnSection[0].origin.x - divdedSection.sectionGrid[0].columnSection[0].origin.x;
            lineOrigin.x = divdedSection.sectionGrid[0].columnSection[0].origin.x;
            lineOrigin.x += cellWidth / 2.0f;
            lineOrigin.y = divdedSection.sectionGrid[0].columnSection[subdividePos].origin.y + (cellDims.y + lineWidth) / 2.0f;
            lineScale = new Vector3(cellWidth + cellDims.x, lineWidth, 1.0f);
        }
        else
        {
            int back = divdedSection.sectionGrid[0].columnSection.Count - 1;
            float cellLength = divdedSection.sectionGrid[0].columnSection[back].origin.y - divdedSection.sectionGrid[0].columnSection[0].origin.y;
            lineOrigin.y = divdedSection.sectionGrid[0].columnSection[0].origin.y;
            lineOrigin.y += cellLength / 2.0f;
            lineOrigin.x = divdedSection.sectionGrid[subdividePos].columnSection[0].origin.x + (cellDims.x + lineWidth) / 2.0f;
            lineScale = new Vector3(lineWidth, cellLength + cellDims.y, 1.0f);
        }
        SpawnShelfLine(lineOrigin, lineScale);

        return true;
    }

    void RandomSubdivide()
    {
        // Select horizontal or vertical sub-division
        bool horizontalSubdivide = Random.value > 0.5f;
        if (DirectionalRandomSubdivide(horizontalSubdivide) == false)
        {
            DirectionalRandomSubdivide(false);
        }
    }

    GameObject SpawnShelfLine(Vector3 pos, Vector3 scale)
    {
        GameObject shelfLine = Instantiate(shelfLinePrefab, pos, Quaternion.identity);
        shelfLine.transform.localScale = scale;
        shelfLineObjs.Add(shelfLine);
        return shelfLine;
    }

    void ClearShelfLines()
    {
        for (int i = 0; i < shelfLineObjs.Count; ++i)
        {
            Destroy(shelfLineObjs[i]);
        }
        shelfLineObjs.Clear();

        sections.Clear();
        GenerateBaseSection();
    }
}

[System.Serializable]
public class GridInfo
{
    public Vector2 gridOrigin;
    public Vector2 gridHorizontalLimits, gridVerticalLimits;
    public Vector2 GridSize()
    {
        return new Vector2(gridVerticalLimits.y - gridVerticalLimits.x, gridHorizontalLimits.y - gridHorizontalLimits.x);
    }
}

[System.Serializable]
public class SectionInfo
{
    public List<ColumnSectionInfo> sectionGrid;

    public Vector2 Dimensions()
    {
        if (sectionGrid.Count == 0) return new Vector2();
        return new Vector2(sectionGrid.Count, sectionGrid[0].columnSection.Count);
    }

    public bool CanSubdivide(bool horizontal)
    {
        return horizontal ? Dimensions().y > 1 : Dimensions().x > 1;
    }

    public SectionInfo()
    {
        sectionGrid = new List<ColumnSectionInfo>();
    }
}

[System.Serializable]
public class ColumnSectionInfo
{
    public List<CellInfo> columnSection;

    public ColumnSectionInfo()
    {
        columnSection = new List<CellInfo>();
    }
}

[System.Serializable]
public class CellInfo
{
    public Vector2 origin;
}

public class GridLineInfo
{
    public float lineOffset;
}

