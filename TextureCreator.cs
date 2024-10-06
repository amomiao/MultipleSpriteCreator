using System.IO;
using UnityEngine;

namespace Tool.SpritesToMultipleSprite
{
    /// <summary>
    /// 用法:
    /// 1.创建后调用<see cref="Add(Color[][])"/>方法加入色块,方法返回值为 色块被放置到的贴图像素位置(左下原点);
    /// 2.全部加入后调用<see cref="Create(string)"/>方法创建贴图.
    /// </summary>
    public class TextureCreator
    {
        /// <summary> 默认颜色 </summary>
        public static Color32 defaultColor = new Color32(0, 0, 0, 0);
        /// <summary> 第二色 占位用 </summary>
        public static Color32 secondColor = new Color32(0, 0, 0, 1);

        /// <summary> 色彩表 </summary>
        public Color[,] colorTable;
        /// <summary> 间隔填充 </summary>
        public int padding = 2;
        /// <summary> 绘制指针 </summary>
        private Vector2Int pointer;

        // 填充是一边的值,对于区域则是两边都要填充
        public Vector2Int PaddingArea => padding * Vector2Int.one * 2;
        // 大小规模
        public int Size => colorTable.GetLength(0);

        public TextureCreator() 
        { 
            colorTable = new Color[1, 1]; 
            pointer = Vector2Int.zero; 
        }
        public TextureCreator(Vector2Int size) 
        { 
            colorTable = new Color[size.x, size.y]; 
            pointer = Vector2Int.zero; 
        }

        /// <summary> 当得到一个合法区域后,如何将移动指针到下一次检测区域的起点 </summary>
        protected void MovePointerToNext(Vector2Int targetPos,Vector2Int drawingAreaSize)
        {
            // 吸附性随便移,移到了非法区域会调用逻辑会纠正
            pointer = targetPos + new Vector2Int(drawingAreaSize.y, 0);
        }

        /// <summary>  </summary>
        /// <param name="name"></param>
        /// <param name="savePath"></param>
        /// <returns> 拼接文件名后的全路径 </returns>
        public string Create(string name = "Texture",string savePath = "")
        {
            Texture2D texture = new Texture2D(colorTable.GetLength(0), colorTable.GetLength(1));
            texture.filterMode = FilterMode.Point;
            for (int i = 0; i < colorTable.GetLength(0); i++)
                for (int j = 0; j < colorTable.GetLength(1); j++)
                {
                    if(colorTable[i, j] != secondColor)
                        texture.SetPixel(i, j, colorTable[i, j]);
                    else
                        texture.SetPixel(i, j, defaultColor);
                }
            texture.Apply();
            // 保存路径空或不存在,写入streamingAssets
            if (string.IsNullOrEmpty(savePath))
                savePath = Application.streamingAssetsPath;
            else if (!Directory.Exists(savePath))
            {
                Debug.LogError($"{savePath} 路径不存在");
                savePath = Application.streamingAssetsPath;
            }
            // 写后缀
            savePath += "/" + name + ".png";
            using (MemoryStream ms = new MemoryStream())
            {
                //得到对象的2进制字节数组
                byte[] bytes = texture.EncodeToPNG();
                //存储字节
                File.WriteAllBytes(savePath, bytes);
                //关闭内存流
                ms.Close();
            }
            Debug.Log(string.Format("成功创建图片于{0}", savePath));
            return savePath;
        }

        /// <summary> 把一块颜色区域加入贴图 </summary>
        /// <returns> 左下锚点 </returns>
        public Vector2Int Add(Color[][] colors)
        {
            Vector2Int size = new Vector2Int(colors.GetLength(0), colors[0].GetLength(0));
            Vector2Int getPos = GetPos(ref size);
            if (getPos != -Vector2Int.one)
            {
                Vector2Int v;
                for (int x = getPos.x; x < getPos.x + size.y; x++)
                    for (int y = getPos.y; y < getPos.y + size.x; y++)
                    {
                        // 这块区域是Padding区域,绘制第二色
                        if (x < getPos.x + padding ||
                            x >= getPos.x + size.y - padding ||
                            y < getPos.y + padding ||
                            y >= getPos.y + size.x - padding)
                        {
                            this.colorTable[x, y] = secondColor;
                        }
                        else
                        {
                            v = new Vector2Int(y - getPos.y - padding, x - getPos.x - padding);
                            // 声明的区域内不存在默认色, 同默认色的像素使用第二色占位
                            // 最后渲染的时候 第二色像素使用默认色
                            if (colors[v.x][v.y] == defaultColor)
                                this.colorTable[x, y] = secondColor;
                            else
                                this.colorTable[x, y] = colors[v.x][v.y];
                        }
                    }
                // 实际的像素位置
                return getPos + new Vector2Int(padding, padding);
            }
            else
            {
                Expand();
                return Add(colors);
            }
        }

        /// <summary> 扩大贴图像素 </summary>
        private void Expand()
        {
            Color[,] newColorTable = new Color[colorTable.GetLength(0) * 2, colorTable.GetLength(1) * 2];
            for (int i = 0; i < colorTable.GetLength(0); i++)
                for (int j = 0; j < colorTable.GetLength(1); j++)
                {
                    newColorTable[i, j] = colorTable[i, j];
                }
            colorTable = newColorTable;
        }

        // ^3的复杂度效率很低,创建区域用个AABB会快很多,不过工具卡个几秒正好多玩会手机
        /// <summary> 指针寻找可绘制区域的逻辑 </summary>
        /// <param name="size"> 需求区域大小,ref使用padding拓展区域 </param>
        /// <returns> 
        /// 可以获得一个合法区域时,返回左下角点;
        /// 不能获得一个合法区域时返回(-1,-1)。
        /// </returns>
        private Vector2Int GetPos(ref Vector2Int size)
        {
            size += PaddingArea;
            // 当前点是否可以进行填色
            bool startable = true;
            // 指针是否是 从原点开始的
            // 从原点开始被认为是绘制溢出,需要申请更大的贴图
            bool isZeroStart;
            // 指针溢出则归零
            if (pointer.x >= colorTable.GetLength(0) && pointer.y >= colorTable.GetLength(1))
                pointer = Vector2Int.zero;
            isZeroStart = pointer == Vector2Int.zero;
            // 指针检测
            // 逐像素检查以这个像素为起点,能否创建一个新区域
            for (int i = pointer.x; i < colorTable.GetLength(0); i++)
            {
                for (int j = pointer.y; j < colorTable.GetLength(1); j++)
                {
                    // 当前点为默认色则为可用点
                    if (colorTable[i, j] == defaultColor)
                    {
                        // 初检: 数组长度上这个区域存在合法可能
                        if (i + size.y < colorTable.GetLength(0) && j + size.x < colorTable.GetLength(1))
                        {
                            // 二检: 逐像素确定是否合法
                            for (int x = i; x < i + size.y; x++)
                                for (int y = j; y < j + size.x; y++)
                                {
                                    // Break Pass
                                    // 点位色彩非默认色则被占用,跳出
                                    if (colorTable[x, y] != defaultColor)
                                    {
                                        startable = false;
                                        break;
                                    }
                                }
                        }
                        else
                            continue;
                        // Return Pass 0
                        // 若可用 开启写入
                        if (startable)
                        {
                            // pointer指针到下一次检测的开始位置
                            MovePointerToNext(new Vector2Int(i, j), size);
                            return new Vector2Int(i, j);
                        }
                    }
                }
            }
            // Return Pass 1
            if (!isZeroStart)
            {
                pointer = Vector2Int.zero;
                size -= PaddingArea;
                return GetPos(ref size);
            }
            // Return Pass 2
            else
            {
                return -Vector2Int.one;
            }
        }
    }
}