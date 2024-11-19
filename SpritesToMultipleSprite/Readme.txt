依赖
	Unity.2D

使用
	1. 将MultipleSpriteCreator挂载到任意物体上
	2. 填写 OutName :输出图片的名称
	3. 填写 Padding :图片-图片/边界-图片间的间隔
	4. 放入图片
		图片设置:
			设置为精灵 
			开启图片的Read/Write
	5. 点击'创建'等待运行完成 选择存储区域

小注:
	1. 图片最好控制在1024尺寸以下。
	2. 非透明区域默认会有一些虚化,可以设置图片的属性'FilterMode'为Point.
		或者MultipleSpriteCreatorEditor修改的逻辑,
			在spriteItems[i] = new SpriteItem(creator.Add(colors), size, Sprites[i].name);

绘制图片的逻辑(TextureCreator)，复杂度很高，n^3。