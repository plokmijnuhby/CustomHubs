[简体中文](README_zh.md) | [English](README.md)

# CustomHubs

此项目为《Patrick's Parabox》游戏的 mod，支持创建自定义 Hub。使用此项目略显复杂，如果我有什么没说清楚的，随时私信我。
## 安装 / Installation
在安装这个 mod 之前请先定位《Patrick's Parabox》游戏的本地文件夹（通常位于 "C:\Program Files (x86)\Steam\steamapps\common\Patrick's Parabox"）。

在此文件夹中通过 [BepInEx 文档](https://docs.bepinex.dev/articles/user_guide/installation/index.html) 的说明安装 BepInEx（Windows 系统上记得根据 32 位 / 64 位选择正确的版本）。

从 [该仓库的 Releases 页面](https://github.com/plokmijnuhby/CustomHubs/releases) 下载 "CustomHubs.dll" 文件，并移至已经存在的 "Patrick's Parabox\BepInEx\plugins" 文件夹下。

将自定义 Hub 文件夹移至 "custom_levels" 文件夹里，即可正常加载。

## 建造自定义 Hub / Making a custom hub
首先说明：我通常不会修复游戏核心机制的漏洞。因为如果修复，一旦做出任何不寻常的操作，就可能导致游戏卡顿或不稳定。

如果你不理解上述解释，参考此项目中名为 test_hub 的完整示例。
### Hub 文件 / The hub file
地图文件是整个 Hub 最重要的文件（技术上讲，唯一需要的文件）。此地图文件应当被命名为 hub.txt。该文件的格式和自定义关卡几乎相同，除了一些小改变。Hub 文件允许使用 Portal，使用如下语法：
```
Floor X坐标 Y坐标 Portal 谜题名称
```
Reference 的语法会稍有不同，最后多了一个参数表示指向的区域（Area，即原版游戏中“入门/Intro”、“进入/Enter”等谜题集）名称。每个区域要有至少一个 Reference 指向它，否则游戏不知道它属于哪个区域。

此外，Wall 也多一个参数，效果如下：
- `_`：普通墙。
- 谜题名称: 表明这个墙会在完成足够的谜题后解锁。注意：墙必须和该谜题相连。
- `*clear`：在玩家看完 Credits 后解锁。
- `*clear_or_unlock_all`：同上，但在设置里开启“解锁所有谜题”也能解锁。
- `*all`: 完成所有谜题后解锁。

对于第二种情况，解锁与谜题相连的墙所需完成的谜题数量，等于该区域主线谜题的数量。（在主游戏中，大多数区域也是如此，但“墙/Wall”和“调换/Swap”两个区域硬编码了不同的所需谜题数量。）

最后，一个 playerButton 会在解锁时触发 Credits。

决定关卡何时解锁的机制较为复杂。如果一个谜题有来自其他谜题的连线指向它，并且第一个指向它的谜题已完成，则该谜题解锁。此外，如果一个区域不在另一个区域内，或者其上方是空的（或是一面已解锁的墙），那么该区域中的部分谜题会自动解锁。这大部分情况下易于理解，但需要注意：没有连线指向的蓝色奖励关卡，只会在该区域所有可解锁的墙（即额外参数不是 `_` 的墙）都解锁后才会解锁。

还有几个重要事项需要注意：
1. 玩家初始位置不应在 Portal 旁边，因为这会干扰退出 Portal 的方式。
2. 重新加载存档时，玩家会被放置在当前区域从上往下数第二行的中心位置，因此每个区域的这个位置都应留空。

### 其他文件 / Other files
你可以将所有其他文件放在子目录中，这样可能有助于整理关卡。此外，如果需要，大部分必需的数据可以拆分到多个文件中，并放置在不同的目录下。不同子目录中名为 "area_data.txt" 的两个文件会被视为首尾相接在一起。

每个谜题都应有一个名为 "{谜题名称}.txt" 的关卡文件。它可以是任何普通的自定义关卡，但必须将 custom_level_music 设置为非 -1 的值。还应有一个图像文件，如 "{谜题名称}.png"，用于在 Portal 上显示图标。如不提供图像，系统会自动生成。

文件 "area_data.txt" 应包含 Hub 中每个区域的条目。每行格式如下：
```
区域名称 音乐ID
```
其中 `音乐ID` 表示该区域的音乐编号（同样不能为 -1）。

文件 "puzzle_data.txt" 更为复杂。应为每个谜题准备一行，格式如下：
```
谜题名称 难度 eyesJump referenceName
```
`难度`：0 代表主线关卡，1 代表挑战关卡（红色描边），2 代表特殊关卡（青色描边）。`eyesJump` 表示玩家是否应该占据（Possess） Portal 而不是跳入其中。`referenceName` 表示关卡的编号。

另一个文件 "puzzle_lines.txt" 指示谜题连接关系。格式如下：
```
起点谜题 终点谜题 是否立即
```
表示连线从名为 `起点谜题` 的谜题指向 `终点谜题`。`是否立即` 表示完成 `起点谜题` 后是否立即前往 `终点谜题`。

最后还剩一个可选文件 "credits.txt"，用于指定完成 Hub 后展示的 Credits 内容。
