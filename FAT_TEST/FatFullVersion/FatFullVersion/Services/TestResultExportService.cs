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
                        
                        // 设置列宽
                        sheet.SetColumnWidth(0, 15 * 256); // 模块名称
                        sheet.SetColumnWidth(1, 10 * 256); // 模块类型
                        sheet.SetColumnWidth(2, 25 * 256); // 变量名称
                        sheet.SetColumnWidth(3, 30 * 256); // 变量描述
                        sheet.SetColumnWidth(4, 15 * 256); // 硬点测试结果
                        sheet.SetColumnWidth(5, 15 * 256); // 测试时间
                        sheet.SetColumnWidth(6, 15 * 256); // 测试值
                        sheet.SetColumnWidth(7, 15 * 256); // 量程下限
                        sheet.SetColumnWidth(8, 15 * 256); // 量程上限
                        
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
                        
                        // 创建标题行
                        var headerRow = sheet.CreateRow(0);
                        var headers = new[] { "模块名称", "模块类型", "变量名称", "变量描述", "测试结果", "测试时间", "测试值", "量程下限", "量程上限" };
                        
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = headerRow.CreateCell(i);
                            cell.SetCellValue(headers[i]);
                            cell.CellStyle = headerStyle;
                        }
                        
                        // 填充数据行
                        int rowIndex = 1;
                        foreach (var result in testResults)
                        {
                            var dataRow = sheet.CreateRow(rowIndex++);
                            
                            // 设置单元格值
                            SetCellValue(dataRow, 0, result.ModuleName, contentStyle);
                            SetCellValue(dataRow, 1, result.ModuleType, contentStyle);
                            SetCellValue(dataRow, 2, result.VariableName, contentStyle);
                            SetCellValue(dataRow, 3, result.VariableDescription, contentStyle);
                            
                            // 测试结果单元格，根据结果设置不同样式
                            var resultCell = dataRow.CreateCell(4);
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
                            
                            // 测试时间
                            string timeText = result.FinalTestTime.HasValue 
                                ? result.FinalTestTime.Value.ToString("yyyy-MM-dd HH:mm:ss") 
                                : "-";
                            SetCellValue(dataRow, 5, timeText, contentStyle);
                            
                            // 实际测试值
                            double testValue = 0;
                            // 尝试将CurrentValue字符串转换为数值
                            if (double.TryParse(result.CurrentValue, out double parsedValue))
                            {
                                testValue = Math.Round(parsedValue, 3);
                            }
                            var valueCell = dataRow.CreateCell(6);
                            valueCell.SetCellValue(testValue);
                            valueCell.CellStyle = contentStyle;
                            
                            // 量程下限值
                            double lowerLimit = double.TryParse(result.RangeLowerLimit, out double lower) ? Math.Round(lower, 3) : 0;
                            var lowerCell = dataRow.CreateCell(7);
                            lowerCell.SetCellValue(lowerLimit);
                            lowerCell.CellStyle = contentStyle;
                            
                            // 量程上限值
                            double upperLimit = double.TryParse(result.RangeUpperLimit, out double upper) ? Math.Round(upper, 3) : 0;
                            var upperCell = dataRow.CreateCell(8);
                            upperCell.SetCellValue(upperLimit);
                            upperCell.CellStyle = contentStyle;
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
    }
} 