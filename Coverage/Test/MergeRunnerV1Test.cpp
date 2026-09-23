#include "CppUnitTest.h"
#include <SDKDDKVer.h>
#include <memory>

#include "MergeRunnerV1.h"

#ifndef NOMINMAX
#	define NOMINMAX
#	include <Windows.h>
#endif

#pragma warning(disable: 4091)
#include <DbgHelp.h>
#pragma warning(default: 4091)

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace TestMergeRunner
{
  TEST_CLASS(TestMergeRunnerV1)
  {
  public:

    TEST_CLASS_CLEANUP(CleanUp)
    {
      FileSystem::DeleteTestFiles();
    }

    TEST_METHOD(SimpleMerge)
    {
      const std::string report1 =
        "FILE: C:\\proj\\src\\srcFile.cpp\n" \
        "RES: cpu_ucp\n" \
        "PROF: \n" \
        "FILE: C:\\proj\\src\\srcFile.hpp\n" \
        "RES: cccc_pppp_uuuu\n" \
        "PROF: \n" \
        "FILE: C:\\proj\\src\\modifiedFile.cpp\n" \
        "RES: ccuupp\n" \
        "PROF: \n";

      const std::string report2 =
        "FILE: C:\\proj\\src\\srcFile.cpp\n" \
        "RES: ucppcup\n" \
        "PROF: \n" \
        "FILE: C:\\proj\\src\\srcFile.hpp\n" \
        "RES: ucpu_cuup_pcpc\n" \
        "PROF: \n" \
        "FILE: C:\\proj\\src\\modifiedFile.cpp\n" \
        "RES: ccuuppc\n" \
        "PROF: \n" \
        "FILE: C:\\proj\\src\\srcFile2.hpp\n" \
        "RES: ccpuuu\n" \
        "PROF: \n";

      const std::string expectReport =
        "FILE: C:\\proj\\src\\modifiedFile.cpp\n" \
        "RES: ccuupp\n" \
        "PROF: \n" \
        "FILE: C:\\proj\\src\\srcFile.cpp\n" \
        "RES: ccp_ccp\n" \
        "PROF: \n" \
        "FILE: C:\\proj\\src\\srcFile.hpp\n" \
        "RES: cccc_cppp_pcpc\n" \
        "PROF: \n" \
        "FILE: C:\\proj\\src\\srcFile2.hpp\n" \
        "RES: ccpuuu\n" \
        "PROF: \n";

      FileSystem::CreateTestFile("report1", report1);
      FileSystem::CreateTestFile("report2", report2);

      MergeRunnerV1 merger;
      merger.merge("report1", "report2");

      std::stringstream ss;
      merger.saveResultToStream(ss);

      Assert::AreEqual(expectReport, ss.str());
    }

    TEST_METHOD(MergedFileNotExist)
    {
      MergeRunnerV1 merger;
      auto func = [&] { merger.merge("report1_notExist", "report2_notExist"); };
      Assert::ExpectException<std::exception>(func);
    }
  };
}