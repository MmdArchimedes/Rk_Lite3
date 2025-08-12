using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 简化版PLY 2D点云地图管理器
/// </summary>
public class PLY2DPointCloudMap : MonoBehaviour
{
    [Header("=== 数据源选择 ===")]
    [Tooltip("选择点云数据的来源类型")]
    public DataSourceType dataSource = DataSourceType.LoadFromPLY;

    [Header("=== PLY文件设置 ===")]
    [Tooltip("可用的PLY文件路径列表")]
    public string[] availablePlyFiles = new string[]
    {
         "Assets/PointCloudData/output_basic.ply",
    };

    [Tooltip("当前选择的PLY文件索引")]
    [Range(0, 10)]
    public int selectedPlyFileIndex = 0;

    [Header("=== 可视化设置 ===")]
    [Tooltip("在Scene视图中显示的点的大小")]
    public float pointSize = 0.05f;

    [Header("=== 简化颜色设置 ===")]
    [Tooltip("可通行区域的颜色(绿色)")]
    public Color passableColor = Color.green;
    [Tooltip("障碍物的颜色(紫色)")]
    public Color obstacleColor = Color.magenta;
    [Tooltip("新添加点的颜色(蓝色)")]
    public Color newPointColor = Color.blue;

    [Header("=== 手动编辑 ===")]
    [Tooltip("是否开启编辑模式（运行时有效）")]
    public bool editMode = false;
    [Tooltip("编辑刷子的大小")]
    [Range(0.1f, 3.0f)]
    public float editBrushSize = 1.0f;
    [Tooltip("添加点时的捕捉距离")]
    [Range(0.1f, 2.0f)]
    public float snapDistance = 0.5f;

    [Header("=== 点云优化设置 ===")]
    [Tooltip("是否启用点云优化（去除重复点）")]
    public bool enablePointOptimization = true;
    [Tooltip("重复点检测的最小距离")]
    [Range(0.01f, 0.5f)]
    public float duplicateDetectionDistance = 0.1f;

    [Header("=== 摄像机控制 ===")]
    [Tooltip("摄像机的固定Z坐标")]
    public float cameraFixedZ = -10f;
    [Tooltip("是否强制保持摄像机固定位置")]
    public bool keepCameraFixed = true;

    [Header("=== 数据保存路径 ===")]
    [Tooltip("保存编辑数据的根目录")]
    public string saveDataPath = "Assets/PointCloudData/SavedMaps/";

    [Header("=== 保存的地图管理 ===")]
    [Tooltip("可用的已保存地图列表")]
    public List<string> availableSavedMaps = new();
    [Tooltip("当前选择的已保存地图索引")]
    [Range(0, 50)]
    public int selectedSavedMapIndex = 0;

    // 数据源类型枚举 - 移除了GenerateDefault
    public enum DataSourceType
    {
        LoadFromPLY,     // 从PLY文件加载
        LoadSavedMap     // 加载已保存的地图
    }

    // 简化的点类型枚举
    public enum PointType
    {
        Passable,    // 可通行区域(绿色)
        Obstacle,    // 障碍物(紫色)
        NewPoint     // 新添加的点(蓝色)
    }

    // 2D点云数据结构
    [System.Serializable]
    public class Point2D
    {
        public Vector2 position;        // 点的2D坐标
        public Color originalColor;     // 原始颜色
        public PointType type;          // 点的类型
        public bool isManuallyEdited;   // 是否被手动编辑过
        public bool isNewlyAdded;       // 是否是新添加的点

        public Point2D(Vector2 pos)
        {
            position = pos;
            originalColor = Color.white;
            type = PointType.Passable;
            isManuallyEdited = false;
            isNewlyAdded = false;
        }

        public Point2D(Vector2 pos, PointType pointType)
        {
            position = pos;
            originalColor = Color.white;
            type = pointType;
            isManuallyEdited = false;
            isNewlyAdded = false;
        }

        public Point2D(Vector2 pos, PointType pointType, bool isNew)
        {
            position = pos;
            originalColor = Color.white;
            type = pointType;
            isManuallyEdited = false;
            isNewlyAdded = isNew;
        }
    }

    // 可序列化的地图数据结构
    [System.Serializable]
    public class MapSaveData
    {
        public string mapName;
        public DataSourceType originalDataSource;
        public List<Point2D> points;
        public string timestamp;

        public MapSaveData()
        {
            points = new List<Point2D>();
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    [HideInInspector]
    public List<Point2D> points = new();

    // 状态跟踪
    private string currentLoadedMapName = "";
    private Camera mainCamera;


    // 路径绘制相关
    private Vector2 pathStartPos;
    private bool isDrawingPath = false;

    [Space]
    [Header("=== 调试信息 ===")]
    [SerializeField] private int totalPoints = 0;
    [SerializeField] private int passablePoints = 0;
    [SerializeField] private int obstaclePoints = 0;
    [SerializeField] private int newPoints = 0;

    void Start()
    {
        mainCamera = Camera.main;
        RefreshSavedMapsList();
        LoadData();
    }

    void Update()
    {
        if (editMode && Application.isPlaying)
        {
            HandleManualEditing();
        }
    }

    #region 核心功能

    /// <summary>
    /// 加载数据 - 根据当前设置的数据源类型加载数据
    /// </summary>
    [ContextMenu("加载数据")]
    public void LoadData()
    {
        points.Clear();

        switch (dataSource)
        {
            case DataSourceType.LoadFromPLY:
                LoadPLYFile();
                string plyFileName = System.IO.Path.GetFileNameWithoutExtension(GetCurrentPlyFilePath());
                currentLoadedMapName = $"PLY_{plyFileName}";
                break;

            case DataSourceType.LoadSavedMap:
                LoadSavedMap();
                break;
        }

        ClassifyPoints();
        UpdateDebugInfo();
        Debug.Log($"数据加载完成: {currentLoadedMapName}, 共{totalPoints}个点");
    }

    /// <summary>
    /// 加载PLY文件
    /// </summary>
    [ContextMenu("加载PLY文件")]
    public void LoadPLYFile()
    {
        string plyFilePath = GetCurrentPlyFilePath();

        if (!File.Exists(plyFilePath))
        {
            Debug.LogError($"PLY文件不存在: {plyFilePath}");
            return;
        }

        Debug.Log($"开始加载PLY文件: {plyFilePath}");
        points.Clear();

        try
        {
            string[] lines = File.ReadAllLines(plyFilePath);
            ParsePLYFile(lines);
            Debug.Log($"成功从PLY文件加载 {points.Count} 个2D点");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载PLY文件失败: {e.Message}");
        }
    }

    /// <summary>
    /// 导出为PLY文件 - 简化版本，不添加归类颜色功能
    /// </summary>
    [ContextMenu("导出为PLY文件")]
    public void ExportToPLY()
    {
        if (!Directory.Exists(saveDataPath))
        {
            Directory.CreateDirectory(saveDataPath);
        }

        string exportFileName = string.IsNullOrEmpty(currentLoadedMapName) ? "ExportedMap" : currentLoadedMapName;
        string exportPath = Path.Combine(saveDataPath, exportFileName + "_export.ply");

        try
        {
            List<string> lines = new()
            {
                // PLY文件头部 
                "ply",
                "format ascii 1.0",
                $"element vertex {points.Count}",
                "property float x",
                "property float y",
                "property float z",
                "property uchar type",
                "end_header"
            };

            // 点数据 - 只保存位置和类型，不保存颜色
            foreach (var point in points)
            {
                lines.Add($"{point.position.x} {point.position.y} 0.0 {(int)point.type}");
            }

            File.WriteAllLines(exportPath, lines.ToArray());
            Debug.Log($"地图已成功导出为PLY文件: {exportPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导出PLY文件失败: {e.Message}");
        }
    }

    /// <summary>
    /// 优化点云分布 - 去除过密的点
    /// </summary>
    [ContextMenu("优化点云分布")]
    public void OptimizePointDistribution()
    {
        if (points.Count == 0) return;

        float minDistance = 0.3f;
        List<Point2D> optimizedPoints = new();
        int removedCount = 0;

        // 手动编辑的点优先级更高
        var sortedPoints = points.OrderByDescending(p => p.isManuallyEdited ? 1 : 0).ToList();

        foreach (var point in sortedPoints)
        {
            bool tooClose = false;
            foreach (var existingPoint in optimizedPoints)
            {
                if (Vector2.Distance(point.position, existingPoint.position) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                optimizedPoints.Add(point);
            }
            else
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            points = optimizedPoints;
            UpdateDebugInfo();
            Debug.Log($"分布优化完成，移除了 {removedCount} 个过密的点");
        }
        else
        {
            Debug.Log("点云分布已经是最优状态");
        }
    }

    /// <summary>
    /// 删除选中的保存地图
    /// </summary>
    [ContextMenu("删除选中的保存地图")]
    public void DeleteSelectedSavedMap()
    {
        if (availableSavedMaps.Count == 0)
        {
            Debug.LogWarning("没有可删除的保存地图");
            return;
        }

        int index = Mathf.Clamp(selectedSavedMapIndex, 0, availableSavedMaps.Count - 1);
        string mapName = availableSavedMaps[index];
        string filePath = Path.Combine(saveDataPath, mapName + ".json");

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"已删除保存地图: {mapName}");
                RefreshSavedMapsList();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"删除保存地图失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"要删除的地图文件不存在: {filePath}");
        }
    }

    /// <summary>
    /// 重置地图 - 清空当前数据
    /// </summary>
    [ContextMenu("重置地图")]
    public void ResetMap()
    {
        points.Clear();
        currentLoadedMapName = "";
        UpdateDebugInfo();
        Debug.Log("地图数据已重置");
    }

    /// <summary>
    /// 强制保存
    /// </summary>
    [ContextMenu("保存地图")]
    public void ForceSave()
    {
        SaveCurrentMap();
    }

    #endregion

    #region 手动编辑功能 - 简化版本

    void HandleManualEditing()
    {
        if (mainCamera == null) return;

        // 获取鼠标世界坐标
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        Vector2 editPos = new(mouseWorldPos.x, mouseWorldPos.y);

        // 左键添加障碍
        if (Input.GetMouseButton(0))
        {
            RemoveObstacleAtPosition(editPos);     
        }
        // 右键移除障碍/恢复为可通行
        else if (Input.GetKey(KeyCode.Z))
        {
            AddObstacleAtPosition(editPos);
        }
        // X键添加新点云
        else if (Input.GetKey(KeyCode.X))
        {
            AddNewPointAtPosition(editPos);
        }
        // C键沿路径添加点（按住C键拖拽）
        else if (Input.GetKey(KeyCode.C))
        {
            HandlePathDrawing(editPos);
        }
        // V键删除点云
        else if (Input.GetKey(KeyCode.V))
        {
            DeletePointsAtPosition(editPos);
        }

        // 释放C键时完成路径绘制
        if (Input.GetKeyUp(KeyCode.C) && isDrawingPath)
        {
            FinishPathDrawing(editPos);
        }

        // Ctrl+S 保存
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
        {
            SaveCurrentMap();
        }
    }

    /// <summary>
    /// 添加障碍物
    /// </summary>
    void AddObstacleAtPosition(Vector2 position)
    {
        bool hasChanges = false;
        for (int i = 0; i < points.Count; i++)
        {
            if (Vector2.Distance(points[i].position, position) <= editBrushSize)
            {
                if (points[i].type != PointType.Obstacle)
                {
                    points[i].type = PointType.Obstacle;
                    points[i].isManuallyEdited = true;
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            UpdateDebugInfo();
        }
    }

    /// <summary>
    /// 移除障碍物，恢复为可通行
    /// </summary>
    void RemoveObstacleAtPosition(Vector2 position)
    {
        bool hasChanges = false;
        for (int i = 0; i < points.Count; i++)
        {
            if (Vector2.Distance(points[i].position, position) <= editBrushSize )
            {
                // 恢复到合适的类型
                if (points[i].isNewlyAdded)
                {
                    points[i].type = PointType.NewPoint;
                }
                else
                {
                    points[i].type = PointType.Passable;
                }

                points[i].isManuallyEdited = false;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            UpdateDebugInfo();
        }
    }

    /// <summary>
    /// X键添加新点云功能
    /// </summary>
    void AddNewPointAtPosition(Vector2 position)
    {
        // 检查位置是否已被占用
        if (IsPositionOccupied(position, snapDistance))
        {
            Debug.Log($"位置 {position} 附近已存在点云，跳过添加");
            return;
        }

        // 创建新点
        Point2D newPoint = new(position, PointType.NewPoint, true)
        {
            originalColor = newPointColor
        };
        points.Add(newPoint);

        UpdateDebugInfo();
        Debug.Log($"已在位置 {position} 添加新点云点");
    }

    /// <summary>
    /// 检查指定位置是否已存在点
    /// </summary>
    bool IsPositionOccupied(Vector2 position, float threshold)
    {
        foreach (var point in points)
        {
            if (Vector2.Distance(point.position, position) < threshold)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// C键路径绘制功能
    /// </summary>
    void HandlePathDrawing(Vector2 currentPos)
    {
        if (!isDrawingPath)
        {
            pathStartPos = currentPos;
            isDrawingPath = true;
            Debug.Log("开始路径绘制");
        }
    }

    /// <summary>
    /// 完成路径绘制
    /// </summary>
    void FinishPathDrawing(Vector2 endPos)
    {
        if (isDrawingPath)
        {
            AddPointsAlongPath(pathStartPos, endPos);
            isDrawingPath = false;
            Debug.Log("路径绘制完成");
        }
    }

    /// <summary>
    /// 沿路径添加点
    /// </summary>
    void AddPointsAlongPath(Vector2 startPos, Vector2 endPos)
    {
        float spacing = 0.3f;
        _ = (endPos - startPos).normalized;
        float distance = Vector2.Distance(startPos, endPos);
        int pointCount = Mathf.RoundToInt(distance / spacing);

        int addedCount = 0;
        for (int i = 0; i <= pointCount; i++)
        {
            float t = (float)i / pointCount;
            Vector2 position = Vector2.Lerp(startPos, endPos, t);

            if (!IsPositionOccupied(position, snapDistance))
            {
                Point2D newPoint = new(position, PointType.NewPoint, true)
                {
                    originalColor = newPointColor
                };
                points.Add(newPoint);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            UpdateDebugInfo();
            Debug.Log($"沿路径添加了 {addedCount} 个点");
        }
    }

    /// <summary>
    /// V键删除点云功能
    /// </summary>
    void DeletePointsAtPosition(Vector2 position)
    {
        int removedCount = 0;
        for (int i = points.Count - 1; i >= 0; i--)
        {
            if (Vector2.Distance(points[i].position, position) <= editBrushSize)
            {
                points.RemoveAt(i);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            UpdateDebugInfo();
            Debug.Log($"删除了 {removedCount} 个点");
        }
    }

    #endregion

    #region 数据处理

    string GetCurrentPlyFilePath()
    {
        if (availablePlyFiles.Length == 0)
        {
            return "Assets/PointCloudData/map.ply";
        }
        int index = Mathf.Clamp(selectedPlyFileIndex, 0, availablePlyFiles.Length - 1);
        return availablePlyFiles[index];
    }

    void ParsePLYFile(string[] lines)
    {
        int vertexCount = 0;
        int dataStartIndex = 0;

        // 解析头部
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.StartsWith("element vertex"))
            {
                string[] parts = line.Split(' ');
                if (parts.Length >= 3)
                {
                    vertexCount = int.Parse(parts[2]);
                }
            }
            else if (line == "end_header")
            {
                dataStartIndex = i + 1;
                break;
            }
            else
                continue;
        }
        // 解析点数据
        int loadedPoints = 0;
        for (int i = dataStartIndex; i < lines.Length && loadedPoints < vertexCount; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            Point2D point = ParsePLYLine(line);
            if (point != null)
            {
                points.Add(point);
                loadedPoints++;
            }
        }
    }

    Point2D ParsePLYLine(string line)
    {
        string[] parts = line.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return null;

        try
        {
            float x = float.Parse(parts[0]);
            float y = float.Parse(parts[1]);
            Point2D point = new(new Vector2(x, y))
            {
                // PLY文件导入时，所有点都设置为不可通行（紫色）
                type = PointType.Obstacle,
                originalColor = obstacleColor
            };

            return point;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"解析PLY行失败: {line}, 错误: {e.Message}");
            return null;
        }
    }

    void ClassifyPoints()
    {
        // PLY文件导入时不进行重新分类，保持全绿色
        if (dataSource == DataSourceType.LoadFromPLY)
        {
            Debug.Log("PLY文件导入完成，所有点保持为可通行状态（绿色）");
            return;
        }

        // 已保存地图不需要重新分类，保持原有状态
        if (dataSource == DataSourceType.LoadSavedMap)
        {
            Debug.Log("已保存地图加载完成，保持原有点类型状态");
            return;
        }
    }

    int CountNeighbors(Vector2 centerPos, float radius)
    {
        int count = 0;
        foreach (var point in points)
        {
            float distance = Vector2.Distance(centerPos, point.position);
            if (distance <= radius && distance > 0.001f)
            {
                count++;
            }
        }
        return count;
    }

    void UpdateDebugInfo()
    {
        totalPoints = points.Count;
        passablePoints = 0;
        obstaclePoints = 0;
        newPoints = 0;

        foreach (var point in points)
        {
            switch (point.type)
            {
                case PointType.Passable: passablePoints++; break;
                case PointType.Obstacle: obstaclePoints++; break;
                case PointType.NewPoint: newPoints++; break;
            }
        }
    }

    Color GetPointColor(Point2D point)
    {
        return point.type switch
        {
            PointType.Passable => passableColor,// 绿色
            PointType.Obstacle => obstacleColor,// 紫色
            PointType.NewPoint => newPointColor,// 蓝色
            _ => passableColor,
        };
    }

    #endregion

    #region 保存/加载地图

    void SaveCurrentMap()
    {
        if (!Directory.Exists(saveDataPath))
        {
            Directory.CreateDirectory(saveDataPath);
        }

        string saveFileName = GenerateSaveFileName();
        MapSaveData saveData = new()
        {
            mapName = saveFileName,
            originalDataSource = dataSource,
            points = points
        };

        string json = JsonUtility.ToJson(saveData, true);
        string filePath = Path.Combine(saveDataPath, saveFileName + ".json");

        try
        {
            File.WriteAllText(filePath, json);
            currentLoadedMapName = saveFileName;
            RefreshSavedMapsList();
            Debug.Log($"地图已保存: {saveFileName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存地图失败: {e.Message}");
        }
    }

    string GenerateSaveFileName()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"Map_{timestamp}";
    }

    void LoadSavedMap()
    {
        if (availableSavedMaps.Count == 0)
        {
            Debug.LogWarning("没有可用的保存地图");
            return;
        }

        int index = Mathf.Clamp(selectedSavedMapIndex, 0, availableSavedMaps.Count - 1);
        string mapName = availableSavedMaps[index];
        string filePath = Path.Combine(saveDataPath, mapName + ".json");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"保存的地图文件不存在: {filePath}");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            MapSaveData saveData = JsonUtility.FromJson<MapSaveData>(json);

            if (saveData != null && saveData.points != null)
            {
                points = saveData.points;
                currentLoadedMapName = mapName;

                Debug.Log($"成功加载保存的地图: {mapName}, 加载了 {points.Count} 个点");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载保存的地图失败: {e.Message}");
        }
    }

    void RefreshSavedMapsList()
    {
        availableSavedMaps.Clear();

        if (!Directory.Exists(saveDataPath))
        {
            Directory.CreateDirectory(saveDataPath);
            return;
        }

        string[] files = Directory.GetFiles(saveDataPath, "*.json");
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            availableSavedMaps.Add(fileName);
        }

        availableSavedMaps.Sort((a, b) => string.Compare(b, a));
    }

    #endregion

    #region 可视化

    void OnDrawGizmos()
    {
        if (points == null || points.Count == 0)
        {
            return;
        }

        foreach (var point in points)
        {
            Gizmos.color = GetPointColor(point);
            Vector3 pos3D = new(point.position.x, 0, point.position.y);

            float size = pointSize;
            Gizmos.DrawSphere(pos3D, size);
        }

        // 绘制正在进行的路径绘制
        if (isDrawingPath && editMode)
        {
            Gizmos.color = Color.yellow;
            Vector3 startPos3D = new(pathStartPos.x, pathStartPos.y, 0);

            if (mainCamera != null)
            {
                Vector3 mouseScreenPos = Input.mousePosition;
                mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
                Vector3 endPos3D = new(mouseWorldPos.x, mouseWorldPos.y, 0);

                Gizmos.DrawLine(startPos3D, endPos3D);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // 在编辑模式下显示工具指示器
        if (editMode && Application.isPlaying && mainCamera != null)
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            Vector3 toolPos = new(mouseWorldPos.x, mouseWorldPos.y, 0);

            // 显示编辑刷子范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(toolPos, editBrushSize);
        }
    }

    #endregion

    #region 碰撞检测接口

    /// <summary>
    /// 检查指定位置是否可以通行
    /// </summary>
    public bool CanMoveToPosition(Vector2 position, float checkRadius = 0.3f)
    {
        foreach (var point in points)
        {
            if (point.type == PointType.Obstacle)
            {
                if (Vector2.Distance(position, point.position) < checkRadius)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// 获取指定位置最近的障碍物距离
    /// </summary>
    public float GetDistanceToNearestObstacle(Vector2 position)
    {
        float minDistance = float.MaxValue;

        foreach (var point in points)
        {
            if (point.type == PointType.Obstacle)
            {
                float distance = Vector2.Distance(position, point.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }

        return minDistance == float.MaxValue ? -1f : minDistance;
    }

    #endregion
}