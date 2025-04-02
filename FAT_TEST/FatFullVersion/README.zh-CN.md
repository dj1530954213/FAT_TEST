# FAT 测试系统

本项目是一个功能验收测试（FAT）系统，用于自动化 PLC 通道的分配和测试。系统采用 MVVM 架构模式，基于 WPF 和 .NET 8.0 开发，使用 Prism 框架实现模块化设计和依赖注入。

## 系统架构

- **MVVM 架构模式**：清晰分离视图、视图模型和模型层
- **依赖注入**：通过 Prism 框架和 DryIoc 实现，在 App.xaml.cs 中注册服务
- **模块化设计**：使用 Prism 模块化功能支持功能扩展
- **服务接口分离**：所有服务都有接口定义(IServices目录)和实现(Services目录)
- **区域化导航**：使用 Prism 的区域导航功能

## 技术栈

- .NET 8.0
- WPF
- Prism 框架
- DryIoc
- MaterialDesignThemes
- NPOI（Excel操作）

## 测试 PLC 配置

当前版本已完善了测试 PLC 配置类，主要包括：

1. **TestPlcConfig 类**：
   - 位于 `Entities` 命名空间下
   - 包含 PLC 品牌类型（Micro850、HollySys_LKS 等）
   - 包含 IP 地址
   - 包含通道与通讯地址的对应关系表（CommentsTables 列表）

2. **ComparisonTable 记录类**：
   - 位于 `Entities.ValueObject` 命名空间下
   - 定义为记录（record），包含通道地址、通讯地址和通道类型

3. **测试 PLC 通道类型枚举**：
   - 位于 `Entities.EntitiesEnum` 命名空间下
   - 包含 AI、AO、DI、DO 四种通道类型

4. **通道映射服务更新**：
   - 修改了 `IChannelMappingService` 接口，添加了使用 TestPlcConfig 进行通道分配的方法
   - 更新了 `ChannelMappingService` 实现，使其支持基于 TestPlcConfig 的通道分配
   - 兼容原有的通道分配方法，保证向后兼容性

## 通道映射功能

通道映射服务提供以下主要功能：

1. **通道分配**：
   - 基于 TestPlcConfig 对象进行 PLC 通道的自动分配
   - 支持 AI、AO、DI、DO 四种通道类型
   - 提供默认测试 PLC 配置，同时支持自定义配置

2. **批次管理**：
   - 支持从通道映射中提取批次信息
   - 批次状态自动更新功能
   - 支持批次测试状态管理

3. **数据导入**：
   - 支持从 Excel 导入通道数据
   - 自动创建通道映射关系

## 使用方式

通道分配可以基于 TestPlcConfig 对象进行，提供了更灵活的配置选项：

```csharp
// 创建测试 PLC 配置
var testPlcConfig = new TestPlcConfig
{
    BrandType = PlcBrandTypeEnum.Micro850,
    IpAddress = "192.168.1.1",
    CommentsTables = new List<ComparisonTable>
    {
        new ComparisonTable("AI1_1", "40101", TestPlcChannelType.AI),
        new ComparisonTable("AO1_1", "40111", TestPlcChannelType.AO),
        // 添加更多通道...
    }
};

// 使用配置分配通道
var result = await _channelMappingService.AllocateChannelsAsync(
    aiChannels, aoChannels, diChannels, doChannels, testPlcConfig);
```

## 项目结构

项目主要包含以下目录结构：

- **Entities**：实体类、值对象和枚举
- **IServices**：服务接口
- **Services**：服务实现
- **Models**：数据模型
- **ViewModels**：视图模型
- **Views**：视图
- **Common**：公共类和扩展方法

## 编码规范与要求

1. **依赖注入**：完全实现依赖注入来解耦所有的服务类功能
2. **接口优先**：当需要扩展服务实现中供外部调用的功能时，首先在接口中添加对应的方法，然后实现这些方法
3. **注释要求**：所有添加的代码都需要添加注释，包括类、属性、方法和字段
4. **错误验证**：每次修改代码之后都需要编译一下验证代码是否存在错误
5. **代码关联**：每次修改代码前都需要先查看整个项目的代码，确认相关的关联关系 