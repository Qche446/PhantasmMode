global using Terraria.ModLoader;
global using Terraria;
global using System;
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using System.IO;
global using Terraria.Audio;
global using Terraria.GameContent.Creative;
global using Terraria.Graphics.Effects;
global using Terraria.ID;
global using System.Reflection;
global using System.Collections.Generic;
global using System.Linq;
global using Monochrome.Core.Services;

// 数学层是 L1 核心**向下**依赖的唯一一个命名空间，而且只依赖它的"纯函数"部分（缓动曲线、确定性随机）。
// 这一条与 README 里"Core 不引用 Common/MonoUtil"的旧说法相反，是刻意的取舍：
// 这两样东西各自只有一份实现比"保持零依赖"重要得多，理由写在 Core/README.md 的 §2 与蓝图 §4.1。
global using Monochrome.Common.MonoUtil;

