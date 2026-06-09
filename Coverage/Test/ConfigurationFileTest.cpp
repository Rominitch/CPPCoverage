#include "CppUnitTest.h"
#include <SDKDDKVer.h>

#include "ConfigurationFile.h"
#include "RuntimeOptions.h"

#ifndef NOMINMAX
#	define NOMINMAX
#	include <Windows.h>
#endif

#pragma warning(disable: 4091)
#include <DbgHelp.h>
#pragma warning(default: 4091)

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace TestConfiguration
{
	TEST_CLASS(TestConfigurationFile)
	{
	public:
		TEST_METHOD(BasicConfiguration)
		{
			const std::filesystem::path workingDir = std::filesystem::current_path().parent_path().parent_path();
			const std::filesystem::path configuration = workingDir / "DataTest" / "BasicConfig.cfg";

			ConfigurationFile file(configuration);

			RuntimeOptions options;
			const auto firstPath = std::filesystem::path("E:\\AnotherPath");
			options.CodePaths.insert(firstPath);

			Assert::IsTrue(options.ExportFormat == RuntimeOptions::Native);
			Assert::IsTrue(options._verboseLevel == VerboseLevel::Trace);

			// Read file and fill
			file.setup(options);

			Assert::IsTrue(options.ExportFormat == RuntimeOptions::NativeV2);
			Assert::IsTrue(options._verboseLevel == VerboseLevel::Warning);

			Assert::IsTrue(options.excludeFilter.size() == 4);
			Assert::IsTrue(std::find(options.excludeFilter.cbegin(), options.excludeFilter.cend(), "moc_") != options.excludeFilter.cend());

			Assert::IsTrue(options.CodePaths.size() == 2); // Keep old and simplify added more
			Assert::IsTrue(std::find(options.CodePaths.cbegin(), options.CodePaths.cend(), firstPath) != options.CodePaths.cend());
		}
	};
}