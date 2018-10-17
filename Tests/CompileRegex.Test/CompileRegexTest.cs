using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace CompileResx.Test {
	public sealed class CompileRegexTest {
		private readonly ITestOutputHelper outputHelper;

		public CompileRegexTest(ITestOutputHelper outputHelper) =>
			this.outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));

		[Theory]
		[MemberData(nameof(OptimizeAndExecuteTestData))]
		[Trait("Category", "Optimization")]
		[Trait("Optimization", "compile regex")]
		public async Task OptimizeAndExecuteTest(string framework) {
			var baseDir = Path.Combine(Environment.CurrentDirectory, framework);
			var outputDir = Path.Combine(baseDir, "testtmp_" + Guid.NewGuid().ToString());
			var inputFile = Path.Combine(baseDir, "CompileRegex.exe");
			var outputFile = Path.Combine(outputDir, "CompileRegex.exe");
			FileUtilities.ClearOutput(outputFile);
			var proj = new ConfuserProject {
				BaseDirectory = baseDir,
				OutputDirectory = outputDir
			};

			proj.Rules.Add(new Rule() {
				new SettingItem<IProtection>("compile regex")
			});

			proj.Add(new ProjectModule() { Path = inputFile });


			var parameters = new ConfuserParameters {
				Project = proj,
				ConfigureLogging = builder => builder.AddProvider(new XunitLogger(outputHelper))
			};

			await ConfuserEngine.Run(parameters);

			Assert.True(File.Exists(outputFile));
			Assert.NotEqual(FileUtilities.ComputeFileChecksum(inputFile), FileUtilities.ComputeFileChecksum(outputFile));

			var info = new ProcessStartInfo(outputFile) {
				RedirectStandardOutput = true,
				UseShellExecute = false
			};
			using (var process = Process.Start(info)) {
				var stdout = process.StandardOutput;
				Assert.Equal("START", await stdout.ReadLineAsync());
				Assert.Equal("1234", await stdout.ReadLineAsync());
				Assert.Equal("valid mail", await stdout.ReadLineAsync());
				Assert.Equal("invalid mail", await stdout.ReadLineAsync());
				Assert.Equal("END", await stdout.ReadLineAsync());
				Assert.Empty(await stdout.ReadToEndAsync());
				Assert.True(process.HasExited);
				Assert.Equal(42, process.ExitCode);
			}

			FileUtilities.ClearOutput(outputFile);
		}

		public static IEnumerable<object[]> OptimizeAndExecuteTestData() {
			foreach (var framework in new string[] { "net20", "net40", "net471" })
					yield return new object[] { framework };
		}
	}
}