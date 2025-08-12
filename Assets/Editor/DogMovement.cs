using UnityEditor;
using UnityEngine;

public class DogMovementPLY2D : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 3f;
    public float collisionRadius = 0.3f;
    public float dogHeight = 0f;

    [Header("PLY地图引用")]
    public PLY2DPointCloudMap plyMap;

    [Header("控制键")]
    public KeyCode upKey = KeyCode.W;
    public KeyCode downKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    [Header("调试显示")]
    public bool showDebugInfo = true;
    public bool showCollisionRadius = true;
    public bool showMovementPath = true;

    private Vector2 currentPos2D;
    private Vector2 inputDirection;
    private Vector2 lastValidPosition;

    // 调试信息
    private float distanceToNearestObstacle = 0f;
    private bool isBlocked = false;

    void Start()
    {
        // 自动寻找PLY地图组件
        if (plyMap == null)
        {
            plyMap = FindObjectOfType<PLY2DPointCloudMap>();
        }

        if (plyMap == null)
        {
            Debug.LogError("未找到PLY2DPointCloudMap组件！请确保场景中有PLY地图。");
            return;
        }

        // 初始化位置
        currentPos2D = new Vector2(transform.position.x, transform.position.y);
        lastValidPosition = currentPos2D;
        UpdatePosition();

        Debug.Log("PLY地图机器狗初始化完成！");
        Debug.Log("控制说明:");
        Debug.Log("- WASD: 移动机器狗");
        Debug.Log("- 绿色区域: 可通行");
        Debug.Log("- 红色区域: 障碍物/边缘");
        Debug.Log("- 紫色区域: 手动添加的障碍");

        // 检查初始位置是否安全
        CheckInitialPosition();
    }

    void CheckInitialPosition()
    {
        if (!plyMap.CanMoveToPosition(currentPos2D, collisionRadius))
        {
            Debug.LogWarning("机器狗初始位置在障碍物中！尝试寻找最近的安全位置...");

            // 简单的安全位置搜索
            Vector2 safePosition = FindNearestSafePosition(currentPos2D);
            if (safePosition != Vector2.zero)
            {
                currentPos2D = safePosition;
                UpdatePosition();
                Debug.Log($"已将机器狗移动到安全位置: {safePosition}");
            }
            else
            {
                Debug.LogError("无法找到安全的初始位置！");
            }
        }
    }

    Vector2 FindNearestSafePosition(Vector2 fromPosition)
    {
        float searchRadius = 1f;
        int searchSteps = 8;

        for (float radius = 0.5f; radius <= searchRadius; radius += 0.1f)
        {
            for (int i = 0; i < searchSteps; i++)
            {
                float angle = (360f / searchSteps) * i * Mathf.Deg2Rad;
                Vector2 testPos = fromPosition + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );

                if (plyMap.CanMoveToPosition(testPos, collisionRadius))
                {
                    return testPos;
                }
            }
        }

        return Vector2.zero; // 未找到安全位置
    }

    void Update()
    {
        if (plyMap == null) return;

        HandleInput();
        TryMove();
        UpdatePosition();
        UpdateDebugInfo();
    }

    void HandleInput()
    {
        inputDirection = Vector2.zero;

        if (Input.GetKey(upKey)) inputDirection.y += 1f;
        if (Input.GetKey(downKey)) inputDirection.y -= 1f;
        if (Input.GetKey(leftKey)) inputDirection.x -= 1f;
        if (Input.GetKey(rightKey)) inputDirection.x += 1f;

        // 标准化方向向量
        if (inputDirection.magnitude > 1f)
            inputDirection = inputDirection.normalized;
    }

    void TryMove()
    {
        if (inputDirection.magnitude == 0f)
        {
            isBlocked = false;
            return;
        }

        // 计算目标位置
        Vector2 targetPos = currentPos2D + inputDirection * moveSpeed * Time.deltaTime;

        // 检查目标位置是否可以到达
        if (plyMap.CanMoveToPosition(targetPos, collisionRadius))
        {
            currentPos2D = targetPos;
            lastValidPosition = currentPos2D;
            isBlocked = false;

            if (showMovementPath)
            {
                Debug.DrawRay(transform.position, new Vector3(inputDirection.x, 0, inputDirection.y), Color.green, 0.1f);
            }
        }
        else
        {
            isBlocked = true;

            if (showMovementPath)
            {
                Debug.DrawRay(transform.position, new Vector3(inputDirection.x, 0, inputDirection.y), Color.red, 0.1f);
            }

            // 尝试滑墙移动
            TrySlideMovement(targetPos);
        }
    }

    void TrySlideMovement(Vector2 blockedTarget)
    {
        // 尝试只在X方向移动
        Vector2 slideX = new Vector2(blockedTarget.x, currentPos2D.y);
        if (plyMap.CanMoveToPosition(slideX, collisionRadius))
        {
            currentPos2D = slideX;
            lastValidPosition = currentPos2D;
            isBlocked = false;
            return;
        }

        // 尝试只在Y方向移动
        Vector2 slideY = new Vector2(currentPos2D.x, blockedTarget.y);
        if (plyMap.CanMoveToPosition(slideY, collisionRadius))
        {
            currentPos2D = slideY;
            lastValidPosition = currentPos2D;
            isBlocked = false;
            return;
        }

        // 如果都无法移动，保持在当前位置
        isBlocked = true;
    }

    void UpdatePosition()
    {
        // 更新3D位置（机器狗悬浮在设定高度）
        transform.position = new Vector3(currentPos2D.x, dogHeight, currentPos2D.y);
    }

    void UpdateDebugInfo()
    {
        if (plyMap != null)
        {
            distanceToNearestObstacle = plyMap.GetDistanceToNearestObstacle(currentPos2D);
        }
    }

    #region 公共接口

    /// <summary>
    /// 传送机器狗到指定位置（如果位置安全）
    /// </summary>
    public bool TeleportTo(Vector2 position)
    {
        if (plyMap.CanMoveToPosition(position, collisionRadius))
        {
            currentPos2D = position;
            lastValidPosition = currentPos2D;
            UpdatePosition();
            Debug.Log($"机器狗已传送到: {position}");
            return true;
        }
        else
        {
            Debug.LogWarning($"无法传送到位置 {position}，该位置被障碍物阻挡");
            return false;
        }
    }

    /// <summary>
    /// 获取机器狗当前2D位置
    /// </summary>
    public Vector2 GetPosition2D()
    {
        return currentPos2D;
    }

    /// <summary>
    /// 检查机器狗当前是否被阻挡
    /// </summary>
    public bool IsBlocked()
    {
        return isBlocked;
    }

    #endregion

    #region Gizmos可视化

    void OnDrawGizmos()
    {
        // 绘制机器狗主体（蓝色立方体）
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);

        if (showCollisionRadius)
        {
            // 绘制碰撞检测范围（黄色圆圈）
            Gizmos.color = isBlocked ? Color.red : Color.yellow;
            Vector3 pos2D = new Vector3(currentPos2D.x, 0, currentPos2D.y);
            Gizmos.DrawWireSphere(pos2D, collisionRadius);

            // 连接线显示2D投影
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pos2D, transform.position);
        }

        // 绘制移动方向
        if (inputDirection.magnitude > 0)
        {
            Gizmos.color = isBlocked ? Color.red : Color.green;
            Vector3 dir3D = new Vector3(inputDirection.x, 0, inputDirection.y);
            Gizmos.DrawRay(transform.position, dir3D * 1.5f);
        }

        // 显示最后有效位置
        Gizmos.color = Color.white;
        Vector3 lastValidPos3D = new Vector3(lastValidPosition.x, 0, lastValidPosition.y);
        Gizmos.DrawWireSphere(lastValidPos3D, 0.1f);
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        // 调试信息显示
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("机器狗调试信息", EditorGUIUtility.isProSkin ?
            new GUIStyle(GUI.skin.label) { normal = { textColor = Color.white } } : GUI.skin.label);

        GUILayout.Label($"当前位置: ({currentPos2D.x:F2}, {currentPos2D.y:F2})");
        GUILayout.Label($"移动方向: ({inputDirection.x:F2}, {inputDirection.y:F2})");
        GUILayout.Label($"移动状态: {(isBlocked ? "被阻挡" : "正常")}");

        if (distanceToNearestObstacle >= 0)
        {
            GUILayout.Label($"最近障碍距离: {distanceToNearestObstacle:F2}");
        }

        if (plyMap != null)
        {
            GUILayout.Label($"地图点数: {plyMap.points.Count}");
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    #endregion
}