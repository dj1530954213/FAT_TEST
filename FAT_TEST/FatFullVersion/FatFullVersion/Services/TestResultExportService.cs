using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FatFullVersion.IServices;
using FatFullVersion.Models;
using Microsoft.Win32;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace FatFullVersion.Services
{
    /// <summary>
    /// 测试结果导出服务实现类 - Excel导出
    /// </summary>
    public class TestResultExportService : ITestResultExportService
    {
        private readonly IMessageService _messageService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="messageService">消息服务</param>
        public TestResultExportService(IMessageService messageService)
        {
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        /// <summary>
        /// 检查是否所有测试点位都已通过
        /// </summary>
        /// <param name="testResults">测试结果数据</param>
        /// <returns>是否所有点位都已通过测试</returns>
        public bool AreAllTestsPassed(IEnumerable<ChannelMapping> testResults)
        {
            if (testResults == null || !testResults.Any())
            {
                return false;
            }

            // 检查是否所有通道的硬点测试结果都是"通过"
            bool allPassed = testResults.All(c => 
                !string.IsNullOrEmpty(c.HardPointTestResult) && 
                (c.HardPointTestResult == "通过" || c.HardPointTestResult == "已通过"));

            return allPassed;
        }

        /// <summary>
        /// 导出测试结果到Excel文件
        /// </summary>
        /// <param name="testResults">测试结果数据</param>
        /// <param name="filePath">导出文件路径，如果为null则通过文件对话框选择</param>
        /// <returns>导出是否成功</returns>
        public async Task<bool> ExportToExcelAsync(IEnumerable<ChannelMapping> testResults, string filePath = null)
        {
            try
            {
                if (testResults == null || !testResults.Any())
                {
                    await _messageService.ShowAsync("导出失败", "没有可导出的测试结果数据", MessageBoxButton.OK);
                    return false;
                }

                // 如果未指定文件路径，则打开保存文件对话框
                if (string.IsNullOrEmpty(filePath))
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Excel文件 (*.xlsx)|*.xlsx",
                        Title = "保存测试结果",
                        DefaultExt = "xlsx",
                        FileName = $"测试结果_{DateTime.Now:yyyyMMdd_HHmmss}"
                    };

                    if (saveFileDialog.ShowDialog() != true)
                    {
                        return false; // 用户取消操作
                    }

                    filePath = saveFileDialog.FileName;
                }

                // 确保有效的文件路径
                if (string.IsNullOrEmpty(filePath))
                {
                    await _messageService.ShowAsync("导出失败", "无效的文件路径", MessageBoxButton.OK);
                    return false;
                }

                // 使用Task.Run在后台线程中执行Excel导出操作
                return await Task.Run(() =>
                {
                    try
                    {
                        // 创建工作簿
                        var workbook = new XSSFWorkbook();
                        
                        // 创建工作表
                        var sheet = workbook.CreateSheet("测试结果");
                        
                        // 设置列宽 - 按照DataEditView.xaml中的DataGrid列进行设置
                        sheet.SetColumnWidth(0, 8 * 256);  // 测试ID
                        sheet.SetColumnWidth(1, 10 * 256); // 测试批次
                        sheet.SetColumnWidth(2, 25 * 256); // 变量名称
                        sheet.SetColumnWidth(3, 10 * 256); // 点表类型
                        sheet.SetColumnWidth(4, 10 * 256); // 数据类型
                        sheet.SetColumnWidth(5, 25 * 256); // 测试PLC通道位号
                        sheet.SetColumnWidth(6, 25 * 256); // 被测PLC通道位号
                        sheet.SetColumnWidth(7, 12 * 256); // 行程最小值
                        sheet.SetColumnWidth(8, 12 * 256); // 行程最大值
                        sheet.SetColumnWidth(9, 12 * 256); // 0%对比值
                        sheet.SetColumnWidth(10, 12 * 256); // 25%对比值
                        sheet.SetColumnWidth(11, 12 * 256); // 50%对比值
                        sheet.SetColumnWidth(12, 12 * 256); // 75%对比值
                        sheet.SetColumnWidth(13, 12 * 256); // 100%对比值
                        sheet.SetColumnWidth(14, 15 * 256); // 低低报反馈状态
                        sheet.SetColumnWidth(15, 15 * 256); // 低报反馈状态
                        sheet.SetColumnWidth(16, 15 * 256); // 高报反馈状态
                        sheet.SetColumnWidth(17, 15 * 256); // 高高报反馈状态
                        sheet.SetColumnWidth(18, 15 * 256); // 维护功能检测
                        sheet.SetColumnWidth(19, 20 * 256); // 开始测试时间
                        sheet.SetColumnWidth(20, 20 * 256); // 最终测试时间
                        sheet.SetColumnWidth(21, 15 * 256); // 测试时长(秒)
                        sheet.SetColumnWidth(22, 25 * 256); // 通道硬点测试结果
                        sheet.SetColumnWidth(23, 25 * 256); // 测试结果
                        
                        // 创建标题行样式
                        var headerStyle = workbook.CreateCellStyle();
                        var headerFont = workbook.CreateFont();
                        headerFont.IsBold = true;
                        headerFont.FontHeightInPoints = 12;
                        headerStyle.SetFont(headerFont);
                        headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                        
                        // 创建内容行样式
                        var contentStyle = workbook.CreateCellStyle();
                        contentStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                        
                        // 创建通过状态样式
                        var passedStyle = workbook.CreateCellStyle();
                        passedStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                        passedStyle.FillForegroundColor = IndexedColors.LightGreen.Index;
                        passedStyle.FillPattern = FillPattern.SolidForeground;
                        
                        // 创建失败状态样式
                        var failedStyle = workbook.CreateCellStyle();
                        failedStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                        failedStyle.FillForegroundColor = IndexedColors.Rose.Index;
                        failedStyle.FillPattern = FillPattern.SolidForeground;
                        
                        // 创建标题行 - 按照DataEditView.xaml中的DataGrid列顺序
                        var headerRow = sheet.CreateRow(0);
                        var headers = new[] { 
                            "测试ID", "测试批次", "变量名称", "点表类型", "数据类型", 
                            "测试PLC通道位号", "被测PLC通道位号", "行程最小值", "行程最大值", 
                            "0%对比值", "25%对比值", "50%对比值", "75%对比值", "100%对比值", 
                            "低低报反馈状态", "低报反馈状态", "高报反馈状态", "高高报反馈状态", "维护功能检测", 
                            "开始测试时间", "最终测试时间", "测试时长", "通道硬点测试结果", "测试结果" 
                        };
                        
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = headerRow.CreateCell(i);
                            cell.SetCellValue(headers[i]);
                            cell.CellStyle = headerStyle;
                        }
                        
                        // 填充数据行 - 按照DataEditView.xaml中的DataGrid列顺序和绑定
                        int rowIndex = 1;
                        foreach (var result in testResults)
                        {
                            var dataRow = sheet.CreateRow(rowIndex++);
                            
                            // 设置单元格值
                            // 1. 测试ID
                            SetCellValue(dataRow, 0, result.TestId.ToString(), contentStyle);
                            
                            // 2. 测试批次
                            SetCellValue(dataRow, 1, result.TestBatch, contentStyle);
                            
                            // 3. 变量名称
                            SetCellValue(dataRow, 2, result.VariableName, contentStyle);
                            
                            // 4. 点表类型
                            SetCellValue(dataRow, 3, result.ModuleType, contentStyle);
                            
                            // 5. 数据类型
                            SetCellValue(dataRow, 4, result.DataType, contentStyle);
                            
                            // 6. 测试PLC通道位号
                            SetCellValue(dataRow, 5, result.TestPLCChannelTag, contentStyle);
                            
                            // 7. 被测PLC通道位号
                            SetCellValue(dataRow, 6, result.ChannelTag, contentStyle);
                            
                            // 8. 行程最小值
                            SetDoubleValue(dataRow, 7, Math.Round(result.RangeLowerLimitValue,3), contentStyle);

                            // 9. 行程最大值
                            SetDoubleValue(dataRow, 8, Math.Round(result.RangeUpperLimitValue, 3), contentStyle);

                            // 10. 0%对比值
                            SetDoubleValue(dataRow, 9, Math.Round(result.Value0Percent, 3), contentStyle);

                            // 11. 25%对比值
                            SetDoubleValue(dataRow, 10, Math.Round(result.Value25Percent, 3), contentStyle);
                            
                            // 12. 50%对比值
                            SetDoubleValue(dataRow, 11, Math.Round(result.Value50Percent, 3), contentStyle);
                            
                            // 13. 75%对比值
                            SetDoubleValue(dataRow, 12, Math.Round(result.Value75Percent, 3), contentStyle);
                            
                            // 14. 100%对比值
                            SetDoubleValue(dataRow, 13, Math.Round(result.Value100Percent, 3), contentStyle);
                            
                            // 15. 低低报反馈状态
                            SetCellValue(dataRow, 14, result.LowLowAlarmStatus, contentStyle);
                            
                            // 16. 低报反馈状态
                            SetCellValue(dataRow, 15, result.LowAlarmStatus, contentStyle);
                            
                            // 17. 高报反馈状态
                            SetCellValue(dataRow, 16, result.HighAlarmStatus, contentStyle);
                            
                            // 18. 高高报反馈状态
                            SetCellValue(dataRow, 17, result.HighHighAlarmStatus, contentStyle);
                            
                            // 19. 维护功能检测
                            SetCellValue(dataRow, 18, result.MaintenanceFunction, contentStyle);
                            
                            // 20. 开始测试时间
                            var testTimeStr = result.TestTime.HasValue 
                                ? result.TestTime.Value.ToString("yyyy-MM-dd HH:mm:ss") 
                                : "-";
                            SetCellValue(dataRow, 19, testTimeStr, contentStyle);
                            
                            // 21. 最终测试时间
                            var finalTestTimeStr = result.FinalTestTime.HasValue 
                                ? result.FinalTestTime.Value.ToString("yyyy-MM-dd HH:mm:ss") 
                                : "-";
                            SetCellValue(dataRow, 20, finalTestTimeStr, contentStyle);
                            
                            // 22. 测试时长(秒)
                            var durationCell = dataRow.CreateCell(21);
                            TimeSpan usedTime = TimeSpan.FromSeconds(Math.Round(result.TotalTestDuration, 0));
                            durationCell.SetCellValue($"{(int)usedTime.Hours:D2}小时{usedTime.Minutes:D2}分{usedTime.Seconds:D2}秒");
                            durationCell.CellStyle = contentStyle;
                            
                            // 23. 通道硬点测试结果
                            var resultCell = dataRow.CreateCell(22);
                            resultCell.SetCellValue(result.HardPointTestResult ?? "未测试");
                            
                            if (!string.IsNullOrEmpty(result.HardPointTestResult) && 
                                (result.HardPointTestResult == "通过" || result.HardPointTestResult == "已通过"))
                            {
                                resultCell.CellStyle = passedStyle;
                            }
                            else
                            {
                                resultCell.CellStyle = failedStyle;
                            }
                            
                            // 24. 测试结果
                            SetCellValue(dataRow, 23, result.ResultText, contentStyle);
                        }
                        
                        // 保存工作簿到文件
                        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            workbook.Write(fs);
                        }
                        
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"导出Excel时出错: {ex.Message}");
                        Application.Current.Dispatcher.Invoke(async () =>
                        {
                            await _messageService.ShowAsync("导出失败", $"导出Excel时出错: {ex.Message}", MessageBoxButton.OK);
                        });
                        return false;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出测试结果出错: {ex.Message}");
                await _messageService.ShowAsync("导出失败", $"导出测试结果出错: {ex.Message}", MessageBoxButton.OK);
                return false;
            }
        }

        /// <summary>
        /// 设置单元格值并应用样式
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="index">列索引</param>
        /// <param name="value">单元格值</param>
        /// <param name="style">单元格样式</param>
        private void SetCellValue(IRow row, int index, string value, ICellStyle style)
        {
            var cell = row.CreateCell(index);
            cell.SetCellValue(value ?? string.Empty);
            cell.CellStyle = style;
        }
        
        /// <summary>
        /// 设置浮点数单元格值并应用样式
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="index">列索引</param>
        /// <param name="value">单元格值</param>
        /// <param name="style">单元格样式</param>
        private void SetDoubleValue(IRow row, int index, double? value, ICellStyle style)
        {
            var cell = row.CreateCell(index);
            if (value.HasValue)
            {
                cell.SetCellValue(value.Value);
            }
            else
            {
                cell.SetCellValue("-");
            }
            cell.CellStyle = style;
        }
    }
} 