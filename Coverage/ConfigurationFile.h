#pragma once

#include <filesystem>

struct RuntimeOptions;

/// <summary>
/// Allow to define all configuration of Coverage (normally done by args)
/// </summary>
class ConfigurationFile
{
	const std::filesystem::path _file;

public:
	ConfigurationFile(const std::filesystem::path& file);

	/// <summary>
	/// Read configuration and edit available option.
	/// The others are leaved unchanged.
	/// </summary>
	/// <param name="option"></param>
	void setup(RuntimeOptions& option);
};