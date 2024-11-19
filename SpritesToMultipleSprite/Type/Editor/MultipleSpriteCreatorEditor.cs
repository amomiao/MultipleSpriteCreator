using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Momos.Tools.SpritesToMultipleSprite
{
    [CustomEditor(typeof(MultipleSpriteCreator))]
    internal class MultipleSpriteCreatorEditor : Editor
    {
        internal class SpriteItem
        {
            // 老版本
            public static implicit operator SpriteMetaData(SpriteItem item)
            {
                SpriteMetaData data = new SpriteMetaData();
                data.rect = new Rect(item.anchorPosition, item.size);
                data.name = item.name;
                data.alignment = (int)SpriteAlignment.Center;
                data.pivot = new Vector2(0.5f, 0.5f);
                return data;
            }
            // 新版本
            public static implicit operator SpriteRect(SpriteItem item)
            {
                SpriteRect data = new SpriteRect();
                data.rect = new Rect(item.anchorPosition, item.size);
                data.name = item.name;
                data.alignment = (int)SpriteAlignment.Center;
                data.pivot = new Vector2(0.5f, 0.5f);
                return data;
            }

            public Vector2Int anchorPosition;
            public Vector2Int size;
            public string name;

            public SpriteItem(Vector2Int anchorPosition, Vector2Int size, string name)
            {
                this.anchorPosition = anchorPosition;
                this.size = size;
                this.name = name;
            }
        }

        private MultipleSpriteCreator _this;
        private TextureCreator creator;
        public int Padding => _this.padding;
        public Sprite[] Sprites => _this.sprites;
        public string OutName => _this.OutName;

        private void OnEnable()
        {
            _this = (MultipleSpriteCreator)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("创建"))
            {
                creator = new TextureCreator();
                creator.padding = Padding;
                Color[][] colors;
                Color[] colColor;
                Vector2Int size;
                // 过时前版本使用SpriteMetaData类型
                SpriteRect[] spriteItems = new SpriteRect[Sprites.Length];
                // 绘制大贴图
                for (int i = 0; i < Sprites.Length; i++)
                {
                    size = new Vector2Int(Sprites[i].texture.width, Sprites[i].texture.height);
                    colors = new Color[size.y][];   // 行数取决于高
                    for (int r = 0; r < colors.GetLength(0); r++)
                    {
                        colColor = new Color[size.x];   // 列数取决于宽
                        for (int c = 0; c < colColor.Length; c++)
                            colColor[c] = Sprites[i].texture.GetPixel(c, r);
                        // 赋值一行像素数据
                        colors[r] = colColor;
                    }
                    // 贴图中绘制精灵项
                    // 使用SpriteItem赋值,自动进行隐式转换
                    spriteItems[i] = new SpriteItem(creator.Add(colors), size, Sprites[i].name);
                }
                string path = EditorUtility.OpenFolderPanel("保存", "", "");
                path = creator.Create(OutName, path);
                // 根据数据制造一个带数据的Multip的Sprite
                AssetDatabase.Refresh();    // 会卡住编辑器进程等待写入完成进行刷新逻辑
                                            // 下面的path参数需要从项目的Assets/开始算路径
                                            // 在不往工程外面放的情况下,path的前面肯定和Application.dataPath全等,把多删的Assets/补回来
                path = "Assets/" + path.Remove(0, Application.dataPath.Length);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                // creator.Create参数的路径填错了会存到StreamingAssets文件夹,下面会报错
                // 因为报错更醒目不判空了
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 100;
                // 已过时
                // importer.spritesheet = spriteItems;  // 未验证
                // 2022版本
                SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
                factory.Init();
                ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
                dataProvider.InitSpriteEditorDataProvider();
                dataProvider.SetSpriteRects(spriteItems);
                dataProvider.Apply();

                // 从此开始通用
                // 重新导入Asset来应用更改
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Success", $"{GetType()} Run Completed", "确定");
                // Debug.Log($"{GetType()} Run Completed");
            }
        }
    }
}