using Microsoft.VisualStudio.TestTools.UnitTesting;
using FatFullVersion.Services;         // Assumes ChannelStateManager is here
using FatFullVersion.Models;           // Assumes ChannelMapping is here, or Entities
using FatFullVersion.IServices;        // For IChannelStateManager and enums
using System;

namespace Fat_UnitTest.Services // Namespace matches your test project
{
    [TestClass]
    public class ChannelStateManagerTests
    {
        private ChannelStateManager _stateManager = null!;
        private DateTime _testTimeNow;

        // String constants from ChannelStateManager for assertions
        private const string StatusNotTested = "未测试";
        private const string StatusWaiting = "等待测试";
        private const string StatusTesting = "测试中";
        private const string StatusPassed = "通过";
        private const string StatusFailed = "失败";
        private const string StatusSkipped = "跳过";
        private const string StatusNotApplicable = "N/A";
        private const string StatusManualTesting = "手动测试中";
        private const string StatusHardPointTesting = "硬点通道测试中";
        private const string StatusHardPointPassed = "硬点通道测试成功";

        [TestInitialize]
        public void TestInitialize()
        {
            _stateManager = new ChannelStateManager();
            _testTimeNow = new DateTime(2024, 1, 1, 12, 0, 0); // Use a fixed time for predictable results
        }

        // Helper to create ExcelPointData with defaults, adjust properties as needed
        private ExcelPointData CreateDefaultExcelPointData(string moduleType = "AI")
        {
            return new ExcelPointData
            {
                VariableName = "TestVar",
                ModuleType = moduleType,
                StationName = "TestStation",
                ChannelTag = "R1_S1_AI_P1",
                RangeLowerLimit = "0",       // Assuming string property in ExcelPointData
                RangeUpperLimit = "100",     // Assuming string property in ExcelPointData
                SLLSetValue = "10",
                SLSetValue = "20",
                SHSetValue = "80",
                SHHSetValue = "90",
                CommunicationAddress = "PLC_Addr_001",
                SLLSetPointCommAddress = "PLC_Addr_SLL",
                SLSetPointCommAddress = "PLC_Addr_SL",
                SHSetPointCommAddress = "PLC_Addr_SH",
                SHHSetPointCommAddress = "PLC_Addr_SHH",
                MaintenanceEnableSwitchPointCommAddress = "PLC_Addr_Maint"
                // Add other properties from ExcelPointData as necessary for initialization logic
            };
        }

        // Helper to create ChannelMapping with defaults
        private ChannelMapping CreateNewChannelMapping(string variableName = "TestVar", string moduleType = "AI")
        {
            return new ChannelMapping 
            { 
                VariableName = variableName, 
                ModuleType = moduleType 
            };
        }

        [TestMethod]
        public void InitializeChannelFromImport_AI_DefaultValues_CorrectlyInitializes()
        {
            var channel = CreateNewChannelMapping(moduleType: "AI");
            var pointData = CreateDefaultExcelPointData(moduleType: "AI");

            _stateManager.InitializeChannelFromImport(channel, pointData, _testTimeNow);

            Assert.AreEqual(0, channel.TestResultStatus);
            Assert.AreEqual(StatusNotTested, channel.ResultText);
            Assert.AreEqual(StatusNotTested, channel.HardPointTestResult);
            Assert.AreEqual(StatusNotTested, channel.ShowValueStatus);
            Assert.AreEqual(StatusNotTested, channel.LowLowAlarmStatus);
            Assert.AreEqual(StatusNotTested, channel.LowAlarmStatus);
            Assert.AreEqual(StatusNotTested, channel.HighAlarmStatus);
            Assert.AreEqual(StatusNotTested, channel.HighHighAlarmStatus);
            Assert.AreEqual(StatusNotTested, channel.AlarmValueSetStatus);
            Assert.AreEqual(StatusNotTested, channel.MaintenanceFunction);
            Assert.AreEqual(StatusNotTested, channel.TrendCheck);
            Assert.AreEqual(StatusNotTested, channel.ReportCheck);
            Assert.AreEqual(0f, channel.RangeLowerLimitValue);
            Assert.AreEqual(100f, channel.RangeUpperLimitValue);
            Assert.AreEqual(10f, channel.SLLSetValueNumber);
        }

        [TestMethod]
        public void InitializeChannelFromImport_AI_EmptyAlarms_SetsAlarmStatusToNA()
        {
            var channel = CreateNewChannelMapping(moduleType: "AI");
            var pointData = CreateDefaultExcelPointData(moduleType: "AI");
            pointData.SLLSetValue = "";
            pointData.SLSetValue = null;
            pointData.SHSetValue = string.Empty;
            pointData.SHHSetValue = " "; // Test whitespace

            _stateManager.InitializeChannelFromImport(channel, pointData, _testTimeNow);

            Assert.AreEqual(StatusNotApplicable, channel.LowLowAlarmStatus);
            Assert.AreEqual(StatusNotApplicable, channel.LowAlarmStatus);
            Assert.AreEqual(StatusNotApplicable, channel.HighAlarmStatus);
            Assert.AreEqual(StatusNotApplicable, channel.HighHighAlarmStatus);
            Assert.AreEqual(StatusNotApplicable, channel.AlarmValueSetStatus, "AlarmValueSetStatus should be N/A if all raw alarm settings are empty/null/whitespace.");
        }

        [TestMethod]
        public void InitializeChannelFromImport_AO_CorrectlyInitializes()
        {
            var channel = CreateNewChannelMapping(moduleType: "AO");
            var pointData = CreateDefaultExcelPointData(moduleType: "AO");

            _stateManager.InitializeChannelFromImport(channel, pointData, _testTimeNow);

            Assert.AreEqual(0, channel.TestResultStatus);
            Assert.AreEqual(StatusNotTested, channel.ResultText);
            Assert.AreEqual(StatusPassed, channel.MaintenanceFunction, "AO MaintenanceFunction should be Passed.");
            Assert.AreEqual(StatusNotApplicable, channel.LowAlarmStatus);
            Assert.AreEqual(StatusNotApplicable, channel.AlarmValueSetStatus);
            Assert.AreEqual(StatusNotTested, channel.TrendCheck);
            Assert.AreEqual(StatusNotTested, channel.ReportCheck);
        }

        [TestMethod]
        public void InitializeChannelFromImport_DI_CorrectlyInitializes()
        {
            var channel = CreateNewChannelMapping(moduleType: "DI");
            var pointData = CreateDefaultExcelPointData(moduleType: "DI");
            pointData.RangeLowerLimit = null; 
            pointData.RangeUpperLimit = "";

            _stateManager.InitializeChannelFromImport(channel, pointData, _testTimeNow);

            Assert.AreEqual(StatusNotApplicable, channel.MaintenanceFunction);
            Assert.AreEqual(StatusNotApplicable, channel.TrendCheck);
            Assert.AreEqual(StatusNotApplicable, channel.ReportCheck);
            Assert.IsNull(channel.Value0Percent);
            Assert.AreEqual(0f, channel.RangeLowerLimitValue); // Per current ChannelStateManager logic for null/empty range
            Assert.AreEqual(100f, channel.RangeUpperLimitValue); // Per current ChannelStateManager logic for null/empty range
        }
        
        [TestMethod]
        public void InitializeChannelFromImport_InvalidRange_ResetsToDefault()
        {
            var channel = CreateNewChannelMapping(moduleType: "AI");
            var pointData = CreateDefaultExcelPointData(moduleType: "AI");
            pointData.RangeLowerLimit = "150";
            pointData.RangeUpperLimit = "50"; // Invalid: Upper < Lower

            _stateManager.InitializeChannelFromImport(channel, pointData, _testTimeNow);

            Assert.AreEqual(0f, channel.RangeLowerLimitValue, "RangeLowerLimit should reset to 0f for invalid range.");
            Assert.AreEqual(100f, channel.RangeUpperLimitValue, "RangeUpperLimit should reset to 100f for invalid range.");
        }

        [TestMethod]
        public void ApplyAllocationInfo_ResetsTestStates()
        {
            var channel = CreateNewChannelMapping();
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData(), _testTimeNow);
            channel.TestResultStatus = 1; channel.ResultText = "Old"; channel.HardPointTestResult = StatusPassed;
            channel.ShowValueStatus = StatusPassed;

            _stateManager.ApplyAllocationInfo(channel, "B1", "T1", "A1");

            Assert.AreEqual("B1", channel.TestBatch);
            Assert.AreEqual(0, channel.TestResultStatus);
            Assert.AreEqual(StatusNotTested, channel.ResultText);
            Assert.AreEqual(StatusNotTested, channel.HardPointTestResult);
            Assert.AreEqual(StatusNotTested, channel.ShowValueStatus, "Sub-test status should reset.");
        }

        [TestMethod]
        public void ClearAllocationInfo_ClearsAndResetsStates()
        {
            var channel = CreateNewChannelMapping();
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData(), _testTimeNow);
            _stateManager.ApplyAllocationInfo(channel, "B1", "T1", "A1");
            _stateManager.BeginHardPointTest(channel, _testTimeNow);
            _stateManager.SetHardPointTestOutcome(channel, new HardPointTestRawResult(true), _testTimeNow);

            _stateManager.ClearAllocationInfo(channel);

            Assert.IsTrue(string.IsNullOrEmpty(channel.TestBatch));
            Assert.AreEqual(0, channel.TestResultStatus);
            Assert.AreEqual(StatusNotTested, channel.ResultText);
        }

        [TestMethod]
        public void MarkAsSkipped_SetsSkippedStatusAndNAForSubtests()
        {
            var channel = CreateNewChannelMapping("AI_SkipTest", "AI");
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData("AI"), _testTimeNow);
            
            _stateManager.MarkAsSkipped(channel, "Test Reason", _testTimeNow);

            Assert.AreEqual(3, channel.TestResultStatus);
            Assert.AreEqual(StatusSkipped, channel.HardPointTestResult);
            Assert.IsTrue(channel.ResultText.Contains("Test Reason"));
            Assert.AreEqual(_testTimeNow, channel.FinalTestTime);
            Assert.AreEqual(StatusNotApplicable, channel.ShowValueStatus);
            Assert.AreEqual(StatusNotApplicable, channel.LowAlarmStatus);
        }

        [TestMethod]
        public void PrepareForWiringConfirmation_FromNotTested_SetsWaiting()
        {
            var channel = CreateNewChannelMapping();
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData(), _testTimeNow);

            _stateManager.PrepareForWiringConfirmation(channel, _testTimeNow);

            Assert.AreEqual(StatusWaiting, channel.HardPointTestResult);
            // 仅确认硬点测试等待状态，ResultText 可以根据 Evaluate 逻辑变化，此处不做严格断言
            StringAssert.Contains(channel.ResultText, StatusWaiting);
            Assert.AreEqual(0, channel.TestResultStatus);
        }

        [TestMethod]
        public void BeginHardPointTest_SetsTestingAndTimes()
        {
            var channel = CreateNewChannelMapping();
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData(), _testTimeNow);
            _stateManager.PrepareForWiringConfirmation(channel, _testTimeNow.AddSeconds(-5));

            _stateManager.BeginHardPointTest(channel, _testTimeNow);

            Assert.AreEqual(StatusTesting, channel.HardPointTestResult);
            StringAssert.Contains(channel.ResultText, StatusHardPointTesting);
            Assert.AreEqual(_testTimeNow, channel.TestTime);
            Assert.AreEqual(_testTimeNow, channel.StartTime);
        }
        
        [TestMethod]
        public void SetHardPointTestOutcome_AI_Success_ManualPending()
        {
            var channel = CreateNewChannelMapping("AI_Test", "AI");
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData("AI"), _testTimeNow);
            _stateManager.BeginHardPointTest(channel, _testTimeNow.AddSeconds(-5));

            _stateManager.SetHardPointTestOutcome(channel, new HardPointTestRawResult(true), _testTimeNow);

            Assert.AreEqual(StatusPassed, channel.HardPointTestResult);
            Assert.AreEqual(0, channel.TestResultStatus, "AI should be 0 (testing) after successful hardpoint as manual tests are pending.");
            StringAssert.Contains(channel.ResultText, StatusManualTesting);
            Assert.IsNull(channel.FinalTestTime);
        }

        [TestMethod]
        public void SetHardPointTestOutcome_Failure_OverallFail()
        {
            var channel = CreateNewChannelMapping();
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData(), _testTimeNow);
            _stateManager.BeginHardPointTest(channel, _testTimeNow.AddSeconds(-5));
            string failDetail = "Connection Error";

            _stateManager.SetHardPointTestOutcome(channel, new HardPointTestRawResult(false, failDetail), _testTimeNow);
            
            Assert.IsTrue(channel.HardPointTestResult.Contains(StatusFailed) && channel.HardPointTestResult.Contains(failDetail));
            Assert.AreEqual(2, channel.TestResultStatus);
            StringAssert.Contains(channel.ResultText, StatusFailed);
            Assert.AreEqual(_testTimeNow, channel.FinalTestTime);
        }

        [TestMethod]
        public void BeginManualTest_ForAI_ResetsPendingSubtests()
        {
            var channel = CreateNewChannelMapping("AI_Manual", "AI");
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData("AI"), _testTimeNow);
            channel.HardPointTestResult = StatusPassed;
            channel.ShowValueStatus = StatusFailed; // Previously failed
            channel.LowAlarmStatus = StatusPassed; // Previously passed, should remain passed
            channel.HighAlarmStatus = StatusNotTested; // Already NotTested
            channel.MaintenanceFunction = StatusNotApplicable; // N/A should remain N/A

            _stateManager.BeginManualTest(channel);

            Assert.AreEqual(StatusNotTested, channel.ShowValueStatus, "Failed ShowValue should be reset to NotTested.");
            Assert.AreEqual(StatusPassed, channel.LowAlarmStatus, "Passed LowAlarm should remain Passed.");
            Assert.AreEqual(StatusNotTested, channel.HighAlarmStatus);
            Assert.AreEqual(StatusNotApplicable, channel.MaintenanceFunction, "N/A MaintenanceFunction should remain N/A.");
            Assert.IsTrue(channel.ResultText.Contains(StatusManualTesting));
            Assert.AreEqual(0, channel.TestResultStatus, "TestResultStatus should be 0 when manual test begins.");
        }

        [TestMethod]
        public void SetManualSubTestOutcome_AIShowValuePass_StillManualTesting()
        {
            var channel = CreateNewChannelMapping("AI_Sub", "AI");
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData("AI"), _testTimeNow);
            channel.HardPointTestResult = StatusPassed;
            _stateManager.BeginManualTest(channel);

            _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.ShowValue, true, _testTimeNow);

            Assert.AreEqual(StatusPassed, channel.ShowValueStatus);
            Assert.AreEqual(0, channel.TestResultStatus);
            Assert.IsTrue(channel.ResultText.Contains(StatusManualTesting));
        }

        [TestMethod]
        public void SetManualSubTestOutcome_AIAllSubtestsPass_OverallPass()
        {
            var channel = CreateNewChannelMapping("AI_AllPass", "AI");
            var pointData = CreateDefaultExcelPointData("AI"); // Ensure all alarms are testable
            _stateManager.InitializeChannelFromImport(channel, pointData, _testTimeNow);
            channel.HardPointTestResult = StatusPassed;
            _stateManager.BeginManualTest(channel);

            _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.ShowValue, true, _testTimeNow);
            if (channel.LowLowAlarmStatus == StatusNotTested) _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.LowLowAlarm, true, _testTimeNow);
            if (channel.LowAlarmStatus == StatusNotTested) _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.LowAlarm, true, _testTimeNow);
            if (channel.HighAlarmStatus == StatusNotTested) _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.HighAlarm, true, _testTimeNow);
            if (channel.HighHighAlarmStatus == StatusNotTested) _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.HighHighAlarm, true, _testTimeNow);
            if (channel.AlarmValueSetStatus == StatusNotTested) _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.AlarmValueSet, true, _testTimeNow);
            if (channel.MaintenanceFunction == StatusNotTested) _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.MaintenanceFunction, true, _testTimeNow);
            if (channel.TrendCheck == StatusNotTested) _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.TrendCheck, true, _testTimeNow);
            if (channel.ReportCheck == StatusNotTested) _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.ReportCheck, true, _testTimeNow);

            Assert.AreEqual(1, channel.TestResultStatus);
            Assert.AreEqual(StatusPassed, channel.ResultText);
            Assert.AreEqual(_testTimeNow, channel.FinalTestTime);
        }

        [TestMethod]
        public void SetManualSubTestOutcome_AISubtestFails_OverallFail()
        {
            var channel = CreateNewChannelMapping("AI_SubFail", "AI");
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData("AI"), _testTimeNow);
            channel.HardPointTestResult = StatusPassed;
            _stateManager.BeginManualTest(channel);
            _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.ShowValue, true, _testTimeNow);
            string failureReason = "Trend data incorrect";

            _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.TrendCheck, false, _testTimeNow, failureReason);

            Assert.AreEqual(StatusFailed, channel.TrendCheck);
            Assert.AreEqual(2, channel.TestResultStatus);
            StringAssert.Contains(channel.ResultText, StatusFailed);
            Assert.AreEqual(_testTimeNow, channel.FinalTestTime);
        }

        [TestMethod]
        public void ResetForRetest_FromFailedState_ResetsToNotTested()
        {
            var channel = CreateNewChannelMapping();
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData(), _testTimeNow);
            _stateManager.SetHardPointTestOutcome(channel, new HardPointTestRawResult(false, "Initial fail"), _testTimeNow.AddMinutes(-5));
            Assert.AreEqual(2, channel.TestResultStatus);

            _stateManager.ResetForRetest(channel);

            Assert.AreEqual(0, channel.TestResultStatus);
            Assert.AreEqual(StatusNotTested, channel.HardPointTestResult);
            Assert.AreEqual(StatusNotTested, channel.ResultText);
            Assert.IsNull(channel.FinalTestTime);
            Assert.IsNull(channel.StartTime);
        }

        [TestMethod]
        public void EvaluateOverallStatus_HardPointPass_ManualItemsNotTested_AI_InProgress()
        {
            // Arrange
            var channel = CreateNewChannelMapping(moduleType: "AI");
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData("AI"), _testTimeNow);
            _stateManager.BeginHardPointTest(channel, _testTimeNow.AddSeconds(-10));
            _stateManager.SetHardPointTestOutcome(channel, new HardPointTestRawResult(true), _testTimeNow); // Hardpoint is Passed
            // Manual items are still StatusNotTested by default after Initialize and BeginHardPointTest's Evaluate

            // Assert
            Assert.AreEqual(StatusPassed, channel.HardPointTestResult);
            Assert.AreEqual(0, channel.TestResultStatus, "TestResultStatus should be 0 (In Progress) as manual tests are pending.");
            StringAssert.Contains(channel.ResultText, StatusManualTesting);
            Assert.IsNull(channel.FinalTestTime, "FinalTestTime should be null.");
        }

        [TestMethod]
        public void MarkAsSkipped_Should_SetSkipStatusAndFinalTime()
        {
            // Arrange
            var channel = CreateNewChannelMapping(moduleType: "AI");
            var skipTime = DateTime.Now;

            // Act
            _stateManager.MarkAsSkipped(channel, "用户选择跳过", skipTime);

            // Assert
            Assert.AreEqual(3, channel.TestResultStatus);
            Assert.AreEqual(StatusSkipped, channel.HardPointTestResult);
            StringAssert.Contains(channel.ResultText, StatusSkipped);
            Assert.IsTrue(channel.FinalTestTime.HasValue);
        }

        [TestMethod]
        public void HardPointAndManualPass_DI_Should_Mark_Overall_Pass()
        {
            // Arrange
            var channel = CreateNewChannelMapping(moduleType: "DI");
            var pointData = CreateDefaultExcelPointData(moduleType: "DI");
            _stateManager.InitializeChannelFromImport(channel, pointData, DateTime.Now);

            // Act
            var startTime = DateTime.Now;
            _stateManager.BeginHardPointTest(channel, startTime);
            _stateManager.SetHardPointTestOutcome(channel, new HardPointTestRawResult(true), DateTime.Now);
            _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.ShowValue, true, DateTime.Now);

            // Assert
            Assert.AreEqual(1, channel.TestResultStatus);
            Assert.AreEqual(StatusPassed, channel.ResultText);
            Assert.IsTrue(channel.FinalTestTime.HasValue);
        }

        [TestMethod]
        public void ManualSubTest_Fail_Should_Mark_Overall_Fail()
        {
            // Arrange
            var channel = CreateNewChannelMapping(moduleType: "AI");
            var pointData = CreateDefaultExcelPointData(moduleType: "AI");
            _stateManager.InitializeChannelFromImport(channel, pointData, DateTime.Now);

            // Act
            _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.ShowValue, false, DateTime.Now, "显示值不一致");

            // Assert
            Assert.AreEqual(2, channel.TestResultStatus);
            StringAssert.Contains(channel.ResultText, "手动测试不通过");
        }

        [TestMethod]
        public void ResetForRetest_Should_Clear_Statuses()
        {
            // Arrange - 先制造一个失败状态
            var channel = CreateNewChannelMapping(moduleType: "AI");
            var pointData = CreateDefaultExcelPointData(moduleType: "AI");
            _stateManager.InitializeChannelFromImport(channel, pointData, DateTime.Now);
            _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.ShowValue, false, DateTime.Now, "显示值不一致");
            Assert.AreEqual(2, channel.TestResultStatus); // 确认已失败

            // Act
            _stateManager.ResetForRetest(channel);

            // Assert
            Assert.AreEqual(0, channel.TestResultStatus);
            Assert.AreEqual(StatusNotTested, channel.HardPointTestResult);
            Assert.AreEqual(StatusNotTested, channel.ShowValueStatus);
            Assert.IsNull(channel.FinalTestTime);
        }

        [TestMethod]
        public void EvaluateOverallStatus_HardPointPass_OneManualItemFails_AI_Fail()
        {
            // Arrange
            var channel = CreateNewChannelMapping(moduleType: "AI");
            _stateManager.InitializeChannelFromImport(channel, CreateDefaultExcelPointData("AI"), _testTimeNow);
            _stateManager.BeginHardPointTest(channel, _testTimeNow.AddSeconds(-10));
            _stateManager.SetHardPointTestOutcome(channel, new HardPointTestRawResult(true), _testTimeNow.AddSeconds(-5)); // Hardpoint is Passed
            _stateManager.BeginManualTest(channel); // Prepares for manual test, sub-items are 'NotTested' or 'N/A'
            _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.ShowValue, true, _testTimeNow.AddSeconds(-4));
            _stateManager.SetManualSubTestOutcome(channel, ManualTestItem.LowAlarm, false, _testTimeNow, "Low Alarm Fail Detail"); // One manual item fails

            // Assert
            Assert.AreEqual(StatusPassed, channel.HardPointTestResult);
            Assert.AreEqual(StatusFailed, channel.LowAlarmStatus);
            Assert.AreEqual(2, channel.TestResultStatus, "TestResultStatus should be 2 (Failed).");
            StringAssert.Contains(channel.ResultText, "手动测试不通过");
            Assert.AreEqual(_testTimeNow, channel.FinalTestTime, "FinalTestTime should be set on failure.");
        }
    }
} 