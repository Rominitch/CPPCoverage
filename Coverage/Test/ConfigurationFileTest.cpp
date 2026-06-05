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

namespace Microsoft
{
	namespace VisualStudio
	{
		namespace CppUnitTestFramework
		{
			template<> static std::wstring ToString<FileCoverageV2::LineArray>(const class FileCoverageV2::LineArray& t) { return L"FileCoverageV2::LineArray"; }
		}
	}
}

namespace TestFormat
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

			Assert::IsTrue(options.ExportFormat == RuntimeOptions::Native);
			Assert::IsTrue(options._verboseLevel == VerboseLevel::Trace);

			// Read file and fill
			file.setup(options);

			Assert::IsTrue(options.ExportFormat == RuntimeOptions::NativeV2);
			Assert::IsTrue(options._verboseLevel == VerboseLevel::Warning);
		}
	};
}