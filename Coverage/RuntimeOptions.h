#pragma once

#include <filesystem>
#include <string>
#include <unordered_set>
#include <vector>

enum class VerboseLevel
{
  Error   = 0x01,
  Warning = 0x03,
  Info    = 0x07,
  Trace   = 0x0F,
  None    = 0
};

struct RuntimeOptions
{
  RuntimeOptions() :
    UseStaticCodeAnalysis(false),
    ExportFormat(Native)
  {}
  virtual ~RuntimeOptions() = default;
  
  VerboseLevel _verboseLevel = VerboseLevel::Trace;

  bool UseStaticCodeAnalysis;

  enum ExportFormatType
  {
    Native,
    NativeV2,
    Cobertura,
    Clover
  } ExportFormat = Native;


  std::string OutputFile;

  std::string MergedOutput;
  std::string WorkingDirectory;
  std::unordered_set<std::filesystem::path> CodePaths;
  std::string Executable;
  std::string ExecutableArguments;
  std::string PackageName = "Program.exe";
  std::string SolutionPath;
  std::vector<std::string> excludeFilter;

  bool isAtLeastLevel(const VerboseLevel& level) const { return (static_cast<int>(_verboseLevel) & static_cast<int>(level)) == static_cast<int>(level); }

  static ExportFormatType toExportFormat(const std::string& t)
  {
    if (t == "native")
    {
      return RuntimeOptions::Native;
    }
    else if (t == "nativeV2")
    {
      return RuntimeOptions::NativeV2;
    }
    else if (t == "cobertura")
    {
      return RuntimeOptions::Cobertura;
    }
    else if (t == "clover")
    {
      return RuntimeOptions::Clover;
    }
    else
    {
      throw std::exception("Unsupported export type. Export type should be cobertura or native.");
    }
  }

  static VerboseLevel toVerbosity(const std::string& lvl)
  {
    if (lvl == "none")
    {
      return VerboseLevel::None;
    }
    else if (lvl == "error")
    {
      return VerboseLevel::Error;
    }
    else if (lvl == "warning")
    {
      return VerboseLevel::Warning;
    }
    else if (lvl == "info")
    {
      return VerboseLevel::Info;
    }
    else if (lvl == "trace")
    {
      return VerboseLevel::Trace;
    }
    else
    {
      throw std::exception(std::format("Unsupported verbose level: {0}.", lvl).c_str());
    }
  }
};

struct RuntimeOptionsSingleton : public RuntimeOptions
{
private:
  RuntimeOptionsSingleton() = default;

public:
  ~RuntimeOptionsSingleton() override = default;

  static RuntimeOptions& Instance()
  {
    static RuntimeOptionsSingleton instance;
    return instance;
  }
};