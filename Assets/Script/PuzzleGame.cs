﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 这个脚本用于控制拼图游戏
/// </summary>
public class PuzzleGame : Puzzle
{
    #region 变量

    [Header("旋转")]

    /// <summary>
    /// 拼图最少的旋转个数
    /// </summary>
    public int minCount = 1;

    /// <summary>
    /// 拼图最少的旋转个数
    /// </summary>
    public int maxCount = 5;

    /// <summary>
    /// 是否旋转
    /// </summary>
    public static bool isRotate;


    /// <summary>
    /// 用户新的 “是否旋转” 选择
    /// </summary>
    bool newIsRotate;

    /// <summary>
    /// 平铺拼图的 起始 位置
    /// </summary>
    Vector3 tileOrigin;


    /// <summary>
    /// 是否显示所有拼图
    /// </summary>
    bool isShowAll;



    /// <summary>
    /// 保存随机数的缓存
    /// </summary>
    List<int> randomBuffer = new List<int>();

    /// <summary>
    /// 新的拼图块数
    /// </summary>
    Vector2 newPieceCount;


    /// <summary>
    /// 游戏是否结束
    /// </summary>
    bool gameFinish;

    /// <summary>
    /// 移动的次数
    /// </summary>
    public int moveCount = 0;

    /// <summary>
    /// 记录开始的时间
    /// </summary>
    public float gameTime = 0;

    /// <summary>
    /// 记录信息
    /// </summary>
    public Record record;

    /// <summary>
    /// 拼图原图对象
    /// </summary>
    GameObject originImage;

    // 是否需要重新开始游戏
    public bool needRestart = false;

    // 游戏结束事件
    public List<EventDelegate> onGameEnd = new List<EventDelegate>();
    #endregion

    #region 重载函数

    /// <summary>
    /// 初始化
    /// </summary>
    protected override void Start()
    {
        base.Start();

        // 停止 时间更新
        Time.timeScale = 0f;

        // 获取原图对象
        originImage = transform.GetChild(0).gameObject;

        //获取平铺的起始位置
        tileOrigin = transform.GetChild(1).position;

    }

    /// <summary>
    /// 固定时间间隔执行
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // 如果 已经开始移动，并且游戏没有结束，就更新并显示时间
        if (moveCount > 0 && !gameFinish)
            gameTime += Time.fixedDeltaTime;
    }

    /// <summary>
    /// 激活拼图对象
    /// </summary>
    /// <param name="go">拼图对象</param>
    protected override void ActiveObject(GameObject go)
    {
        base.ActiveObject(go);

        // 移动次数 加 1
        moveCount++;
    }

    /// <summary>
    /// 取消激活拼图对象
    /// </summary>
    /// <param name="go">拼图对象</param>
    protected override void DeactiveObject(GameObject go)
    {
        base.DeactiveObject(go);

        // 如果游戏结束，返回
        if (gameFinish) return;

        // 是否和所有块相连
        Piece piece = go.GetComponent<Piece>();
        if (piece.connectedPieces.Count == pieceTotalCount - 1)
        {
            // 暂停时间更新
            Time.timeScale = 0;

            // 需要重新开始游戏
            needRestart = true;

            // 保存记录
            record = new Record(pieceTotalCount, moveCount, gameTime, isRotate);

            // 重置时间
            gameTime = 0;

            // 通知游戏结束时间
            for (int i = 0; i < onGameEnd.Count; i++)
                onGameEnd[i].Execute();
        }
    }

    #endregion

    #region 公共函数

    #region 游戏控制

    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {
        // 如果不需要重新开始游戏，并且拼图没有创建
        if (!needRestart && pieceCreated)
        {
            // 继续游戏
            Continue();
            return;
        }

        ReStart();
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void ReStart()
    {
        // 清理拼图
        ClearPiece();

        // 是否更改了拼图个数
        if (pieceCount != newPieceCount)
        {
            // 更新拼图大小
            pieceCount = newPieceCount;
            ReSize();
        }

        // 生成拼图
        MakePuzzle();

        // 切换拼图显示
        ShowAllOrNot(isShowAll);

        // 旋转拼图
        RotatePiece();

        // 重置移动次数
        moveCount = 0;

        // 清除游戏结束标志
        gameFinish = false;

        // 恢复时间更新
        Time.timeScale = 1f;

        // 可以继续游戏
        needRestart = false;
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void Pause()
    {
        // 停止时间更新
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    public void Continue()
    {
        // 恢复时间更新
        Time.timeScale = 1f;
    }
    #endregion

    #region 拼图控制

    /// <summary>
    // 平铺拼图
    /// </summary>
    public void TilePiece()
    {
        // 如果没有生成拼图，返回
        if (!pieceCreated) return;

        // 生成随机数缓存
        BuildRandomBuffer(firstPieceIndex, firstPieceIndex + pieceTotalCount);

        // 每列最多的个数
        int maxVCount = (int)(pieceCount.y * 1.2f);

        // 当前平铺的个数
        int count = 0;

        // 遍历所有拼图
        for (int i = 0; i < pieceTotalCount; i++)
        {
            // 随机获取拼图
            GameObject child = transform.GetChild(randomBuffer[i]).gameObject;

            // 如果已经隐藏，跳过这一个
            if (!child.activeSelf) continue;

            // 是否没有和其它块相连
            if (child.GetComponent<Piece>().connectedPieces.Count == 0)
            {
                // 计算 平铺的 行 和 列
                int x = count / maxVCount;
                int y = count % maxVCount;

                // 移动到新位置
                child.transform.position = tileOrigin +
                    new Vector3(x * displaySize.x * 1.2f,
                                y * displaySize.y * 1.2f, 0);
                // 计数 加 1
                count++;
            }
        }
    }


    /// <summary>
    /// 设置拼图块数
    /// </summary>
    /// <param name="count">新的拼图块数</param>
    public void SetPieceCount(Vector2 count)
    {
        // 保存新的块数
        newPieceCount = count;

        // 游戏是否未开始
        if (!pieceCreated)
        {
            // 设置成新的块数
            pieceCount = newPieceCount;

            // 更新拼图大小
            ReSize();
            return;
        }

        // 判断是否需要重新开始游戏
        needRestart = newPieceCount != pieceCount;
    }

    /// <summary>
    /// 设置拼图形状
    /// </summary>
    /// <param name="name">新的拼图形状名字</param>
    public void SetPieceShape(string name)
    {
        // 构建完整的路径名
        string markName = "Image/puzzle mark/" + name;

        // 是否包含风格
        if (markImage != null && markImage.name.IndexOf('-') != -1)
            markName += "-" + markImage.name.Split('-')[1];

        // 加载新的文件
        markImage = Resources.Load<Texture>(markName);

        // 如果已经创建拼图，更新显示
        if (pieceCreated) UpdatePieceMark();
    }

    /// <summary>
    /// 切换拼图旋转
    /// </summary>
    /// <param name="rotate">是否旋转</param>
    public void ToggleRotate(bool rotate)
    {
        // 保存 旋转设置
        newIsRotate = rotate;

        // 如果没有生成拼图，更新旋转设置
        if (!pieceCreated)
        {
            // 更新旋转设置
            isRotate = newIsRotate;
            return;
        }

        // 判断是否需要重新开始游戏
        needRestart = newIsRotate != isRotate;
    }

    /// <summary>
    /// 设置拼图图像
    /// </summary>
    /// <param name="name">新的拼图图像名字</param>
    public void SetPieceImage(string name)
    {
        // 加载新的拼图图像
        pieceImage = Resources.Load<Sprite>("Image/" + name);

        // 更新拼图原图的显示
        transform.GetChild(0).gameObject.GetComponent<Renderer>().material.SetTexture("_MainTex", pieceImage.texture);

        // 更新拼图的大小
        ReSize();

        // 如果已经创建拼图，更新所有拼图图像
        if (pieceCreated) UpdatePieceImage();

    }

    /// <summary>
    /// 设置拼图风格
    /// </summary>
    /// <param name="name">新的拼图风格名字</param>
    public void SetPieceStyle(string name)
    {
        // 是否已经加载拼图形状
        if (markImage == null) return;

        // 是否禁用风格
        if (name == "none")
            name = markImage.name.Split('-')[0];
        else
            name = markImage.name.Split('-')[0] + "-" + name;

        // 是否和当前的拼图形状名字不一样
        if (name != markImage.name)
        {
            // 加载新的文件
            markImage = Resources.Load<Texture>("Image/puzzle mark/" + name);

            // 如果已经创建拼图，更新所有拼图形状
            if (pieceCreated) UpdatePieceMark();
        }
    }

    /// <summary>
    /// 转换拼图的显示
    /// </summary>
    /// <param name="show">是否显示所有拼图</param>
    public void ShowAllOrNot(bool show)
    {
        // 保存显示状态
        isShowAll = show;

        // 如果没有生成拼图，返回
        if (!pieceCreated) return;

        // 遍历所有拼图
        for (int i = 0; i < pieceCount.x; i++)
            for (int j = 0; j < pieceCount.y; j++)
            {
                // 获取 第 (x,y) 个拼图
                GameObject child = GetPiece(i, j);

                // 判断是否在边界上
                if (!child.GetComponent<Piece>().pid.isAtEdge)
                {
                    // 切换拼图的显示
                    child.SetActive(show);
                }
            }
    }

    #endregion


    /// <summary>
    /// 切换拼图原图的显示
    /// </summary>
    /// <param name="show">是否显示原图</param>
    public void ToggleImage(bool show)
    {
        // 切换 图像的显示
        originImage.SetActive(show);
    }
    #endregion

    #region 其它函数


    /// <summary>
    /// 生成随机数，并放在缓存中，保证不会有重复的数
    /// </summary>
    /// <param name="min">随机数的最小值</param>
    /// <param name="max">随机数的最大值，不包括在内</param>
    void BuildRandomBuffer(int min, int max)
    {
        // 清理缓存
        randomBuffer.Clear();

        // 按递增顺序 初始化列表
        for (int i = min; i < max; i++)
            randomBuffer.Add(i);

        // 随机化列表
        for (int i = 0; i < randomBuffer.Count; i++)
        {
            // 随机选择一个数
            int index = Random.Range(0, randomBuffer.Count);

            // 交换当前的数和 选择的数
            int temp = randomBuffer[i];
            randomBuffer[i] = randomBuffer[index];
            randomBuffer[index] = temp;
        }
    }


    /// <summary>
    /// 更新拼图的形状
    /// </summary>
    void UpdatePieceMark()
    {
        // 遍历所有拼图
        for (int i = 0; i < pieceCount.x; i++)
            for (int j = 0; j < pieceCount.y; j++)
            {
                // 设置 拼图 为新的形状
                GetPiece(i, j).GetComponent<Renderer>().material.SetTexture("_MarkTex", markImage);
            }
    }

    /// <summary>
    /// 更新拼图的图像
    /// </summary>
    void UpdatePieceImage()
    {
        // 遍历所有拼图
        for (int i = 0; i < pieceCount.x; i++)
            for (int j = 0; j < pieceCount.y; j++)
            {
                // 获取第 (x,y) 个拼图
                GameObject child = GetPiece(i, j);

                // 设置 拼图 为新的图像
                child.GetComponent<SpriteRenderer>().sprite = pieceImage;

                // 更新 拼图 的大小
                child.GetComponent<Piece>().ReSize();
            }
    }

    /// <summary>
    /// 旋转拼图
    /// </summary>
    void RotatePiece()
    {
        // 是否更改了旋转设置
        if (isRotate != newIsRotate)
            isRotate = newIsRotate;

        // 如果 不旋转，返回
        if (!isRotate) return;

        // 随机选择个数
        int count = Random.Range(minCount, maxCount);
        for (int i = 0; i < count; i++)
        {
            // 随机角度
            float angle = Random.Range(1, 4) * 90;

            // 随机选择拼图
            GameObject child = GetPiece(Random.Range(0, pieceCount.x), Random.Range(0, pieceCount.y));

            // 旋转拼图
            child.transform.localEulerAngles = new Vector3(0, 0, angle);

            print(child.GetComponent<Piece>() + " " + angle);
        }
    }

    #endregion
}

/// <summary>
/// 游戏记录
/// </summary>
public class Record
{
    /// <summary>
    /// 拼图的块数
    /// </summary>
    public int count;

    /// <summary>
    /// 移动的次数
    /// </summary>
    public int step;

    /// <summary>
    /// 使用的时间
    /// </summary>
    public float time;

    /// <summary>
    /// 是否旋转
    /// </summary>
    bool rotate;

    /// <summary>
    /// 重载 ToString 函数
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return "拼图块数：" + count + (rotate ? ",旋转" : "") + " 移动次数：" + step + " 使用时间：" + time.ToString("F2");
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="_count">拼图块数</param>
    /// <param name="_step">移动次数</param>
    /// <param name="_time">使用时间</param>
    /// <param name="_rotate">是否旋转，默认为否</param>
    public Record(int _count, int _step, float _time, bool _rotate = false)
    {
        // 拼图块数
        count = _count;

        // 移动次数
        step = _step;

        // 使用时间
        time = _time;

        // 是否旋转
        rotate = _rotate;
    }
}