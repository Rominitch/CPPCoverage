
#include "ConfigurationFile.h"

#include "RuntimeOptions.h"

#include <cassert>
#include <format>
#include <fstream>
#include <regex>
#include <vector>

static std::string rtrim(const std::string& str, const char* t = " \t\n\r\f\v")
{
	auto s = str;
	s.erase(s.find_last_not_of(t) + 1);
	return s;
}

ConfigurationFile::ConfigurationFile(const std::filesystem::path& file) :
	_file(file)
{
	if (!std::filesystem::exists(_file))
	{
		throw std::runtime_error(std::format("the filepath is not valid: {0}.", file.string()));
	}
}

enum class GroupID : uint8_t
{
	Unknown,
	General,
	ExcludeFile,
};

void ConfigurationFile::setup(RuntimeOptions& options)
{
	// Open File
	std::ifstream file;
	file.open(_file, std::ifstream::in);
	assert(file.is_open());

	std::regex re(R"(^\s*([^=\s]+)\s*=\s*(.+)$)");
	std::regex reGroup(R"(^\s*\[\s*([^\[\]\s]*)\s*]\s*$)");

	auto group = GroupID::Unknown;

	// Parse file line by line
	std::string line;
	while (std::getline(file, line))
	{
		// Skip comment
		if (line.starts_with("#"))
		{
			continue;
		}
		
		// Try to read group
		std::smatch base_match;
		if (std::regex_match(line, base_match, reGroup) && base_match.size() == 1)
		{
			const auto groupStr = base_match[0].str();

			if(groupStr == "General")
			{
				group = GroupID::General;
			}
			else if (groupStr == "ExcludeFile")
			{
				group = GroupID::ExcludeFile;
			}
			else
			{
				throw std::runtime_error(std::format("Impossible to analyze group: {0}", groupStr));
			}
			continue;
		}
		
		// Try to make action inside group
		switch(group)
		{
			case GroupID::General:
			{
				// Read mono argument

				if (std::regex_match(line, base_match, re) && base_match.size() == 2)
				{
					// Remove possible space after value
					const auto value = rtrim(base_match[1].str());

					// Check configuration
					if (base_match[0].str() == "VERBOSITY")
					{
						options._verboseLevel = RuntimeOptions::toVerbosity(value);
					}
					else if (base_match[0].str() == "EXPORT_FORMAT")
					{
						options.ExportFormat = RuntimeOptions::toExportFormat(value);
					}
					else
					{
						throw std::runtime_error(std::format("Impossible to analyze argument: {0}", base_match[0].str()));
					}
				}
			}
			break;
			case GroupID::ExcludeFile:
			{
				options.excludeFilter.emplace_back(line);
			}
			break;
		}
	}
}