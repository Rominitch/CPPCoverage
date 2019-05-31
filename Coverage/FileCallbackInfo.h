#pragma once

#include "FileLineInfo.h"
#include "FileInfo.h"
#include "BreakpointData.h"
#include "RuntimeOptions.h"
#include "CallbackInfo.h"
#include "ProfileNode.h"
#include "RuntimeNotifications.h"

#include <string>
#include <set>
#include <unordered_map>
#include <memory>
#include <Windows.h>
#include <ctime>

// C++ 17
#include <filesystem>

struct FileCallbackInfo
{
	FileCallbackInfo(const std::string& filename) :
		filename(filename)
	{
		auto& opts = RuntimeOptions::Instance();
		if (opts.CodePath.size() == 0)
		{
			auto idx = filename.find("x64");
			if (idx == std::string::npos)
			{
				idx = filename.find("Debug");
			}
			if (idx == std::string::npos)
			{
				idx = filename.find("Release");
			}
			if (idx == std::string::npos)
			{
				idx = filename.find('\\');
			}
			if (idx == std::string::npos)
			{
				throw "Cannot locate source file base for this executable";
			}
			sourcePath = filename.substr(0, idx);
		}
		else
		{
			sourcePath = opts.CodePath;
		}
	}

	std::string filename;
	std::string sourcePath;

	std::unordered_map<std::string, std::unique_ptr<FileInfo>> lineData;

	void Filter(RuntimeNotifications& notifications)
	{
		std::unordered_map<std::string, std::unique_ptr<FileInfo>> newLineData;
		for (auto& it : lineData)
		{
			if (!notifications.IgnoreFile(it.first))
			{
				std::unique_ptr<FileInfo> tmp(nullptr);
				std::swap(tmp, it.second);
				newLineData[it.first] = std::move(tmp);
			}
			else
			{
				std::cout << "Removing file " << it.first << std::endl;
			}
		}
		std::swap(lineData, newLineData);
	}

	bool PathMatches(const char* filename)
	{
		const char* ptr = filename;
		const char* gt = sourcePath.data();
		const char* gte = gt + sourcePath.size();

		for (; *ptr && gt != gte; ++ptr, ++gt)
		{
			char lhs = tolower(*gt);
			char rhs = tolower(*ptr);
			if (lhs != rhs) { return false; }
		}

		return true;
	}

	FileLineInfo *LineInfo(const std::string& filename, DWORD64 lineNumber)
	{
		auto it = lineData.find(filename);
		if (it == lineData.end())
		{
			auto newLineData = new FileInfo(filename);
			lineData[filename] = std::unique_ptr<FileInfo>(newLineData);

			return newLineData->LineInfo(size_t(lineNumber));
		}
		else
		{
			return it->second->LineInfo(size_t(lineNumber));
		}
	}

	void WriteReport(RuntimeOptions::ExportFormatType exportFormat, std::unordered_map<std::string, std::unique_ptr<std::vector<ProfileInfo>>>& mergedProfileInfo, const std::string& filename)
	{
		switch (exportFormat)
		{
			case RuntimeOptions::Clover: WriteClover(filename); break;
			case RuntimeOptions::Cobertura: WriteCobertura(filename); break;
			default: WriteNative(filename, mergedProfileInfo); break;
		}
	}

	void WriteClover(const std::string& filename)
	{
		std::string reportFilename = filename;
		std::ofstream ofs(reportFilename);

		size_t totalFiles = 0;
		size_t coveredFiles = 0;
		size_t totalLines = 0;
		size_t coveredLines = 0;
		for (auto& it : lineData)
		{
			auto ptr = it.second.get();
			for (size_t i = 0; i < ptr->numberLines; ++i)
			{
				if (ptr->relevant[i] && ptr->lines[i].DebugCount != 0)
				{
					++totalFiles;
					if (ptr->lines[i].HitCount == ptr->lines[i].DebugCount)
					{
						++coveredFiles;
					}

					totalLines += ptr->lines[i].DebugCount;
					coveredLines += ptr->lines[i].HitCount;
				}
			}
		}

		time_t t = time(0);   // get time now
		ofs << "<?xml version=\"1.0\" encoding=\"utf-8\"?>" << std::endl;
		ofs << "<clover generated=\"" << t << "\"  clover=\"3.1.5\">" << std::endl;
		ofs << "<project timestamp=\"" << t << "\">" << std::endl;
		ofs << "<metrics classes=\"0\" files=\"" << totalFiles << "\" packages=\"1\"  loc=\"" << totalLines << "\" ncloc = \"" << coveredLines << "\" ";
		// ofs << "coveredstatements=\"300\" statements=\"500\" coveredmethods=\"50\" methods=\"80\" ";
		// ofs << "coveredconditionals=\"100\" conditionals=\"120\" coveredelements=\"900\" elements=\"1000\" ";
		ofs << "complexity=\"0\" />" << std::endl;
		ofs << "<package name=\"Program.exe\">" << std::endl;
		for (auto& it : lineData)
		{
			auto ptr = it.second.get();

			ofs << "<file name=\"" << it.first << "\">" << std::endl;

			for (size_t i = 0; i < ptr->numberLines; ++i)
			{
				if (ptr->relevant[i] && ptr->lines[i].DebugCount != 0)
				{
					if (ptr->lines[i].HitCount == ptr->lines[i].DebugCount)
					{
						ofs << "<line num=\"" << i << "\" count=\"1\" type=\"stmt\"/>" << std::endl;
					}
					else
					{
						ofs << "<line num=\"" << i << "\" count=\"0\" type=\"stmt\"/>" << std::endl;
					}
				}
			}

			ofs << "</file>" << std::endl;
		}

		ofs << "</package>" << std::endl;
		ofs << "</project>" << std::endl;
		ofs << "</clover>" << std::endl;
	}

	void WriteCobertura(const std::string& filename)
	{
		std::string reportFilename = filename;
		std::ofstream ofs(reportFilename);

		double total = 0;
		double covered = 0;
		for (auto& it : lineData)
		{
			auto ptr = it.second.get();
			for (size_t i = 0; i < ptr->numberLines; ++i)
			{
				if (ptr->relevant[i] && ptr->lines[i].DebugCount != 0)
				{
					++total;
					if (ptr->lines[i].HitCount == ptr->lines[i].DebugCount)
					{
						++covered;
					}
				}
			}
		}

		double lineRate = covered / total;

		ofs << "<?xml version=\"1.0\" encoding=\"utf-8\"?>" << std::endl;
		ofs << "<coverage line-rate=\"" << lineRate << "\">" << std::endl;
		ofs << "<packages>" << std::endl;
		ofs << "<package name=\"Program.exe\" line-rate=\"" << lineRate << "\">" << std::endl;
		ofs << "<classes>" << std::endl;
		for (auto& it : lineData)
		{
			auto ptr = it.second.get();

			std::string name = it.first;
			auto idx = name.find_last_of('\\');
			if (idx != std::string::npos)
			{
				name = name.substr(idx + 1);
			}

			double total = 0;
			double covered = 0;
			for (size_t i = 0; i < ptr->numberLines; ++i)
			{
				if (ptr->relevant[i] && ptr->lines[i].DebugCount != 0)
				{
					++total;
					if (ptr->lines[i].HitCount == ptr->lines[i].DebugCount)
					{
						++covered;
					}
				}
			}

			double lineRate = covered / total;

			ofs << "<class name=\"" << name << "\" filename=\"" << it.first.substr(2) << "\" line-rate=\"" << lineRate << "\">" << std::endl;
			ofs << "<lines>" << std::endl;

			for (size_t i = 0; i < ptr->numberLines; ++i)
			{
				if (ptr->relevant[i] && ptr->lines[i].DebugCount != 0)
				{
					if (ptr->lines[i].HitCount == ptr->lines[i].DebugCount)
					{
						ofs << "<line number=\"" << i << "\" hits=\"1\"/>" << std::endl;
					}
					else
					{
						ofs << "<line number=\"" << i << "\" hits=\"0\"/>" << std::endl;
					}
				}
			}

			ofs << "</lines>" << std::endl;
			ofs << "</class>" << std::endl;
		}

		ofs << "</classes>" << std::endl;
		ofs << "</package>" << std::endl;
		ofs << "</packages>" << std::endl;
		ofs << "<sources><source>c:</source></sources>" << std::endl;
		ofs << "</coverage>" << std::endl;
	}

    std::filesystem::path relativePath(const std::filesystem::path &path, const std::filesystem::path &relative_to)
    {
        // create absolute paths
        std::filesystem::path p = std::filesystem::absolute(path);
        std::filesystem::path r = std::filesystem::absolute(relative_to);

        // if root paths are different, return absolute path
        if(p.root_path() != r.root_path())
            return p;

        // initialize relative path
        std::filesystem::path result;

        // find out where the two paths diverge
        std::filesystem::path::const_iterator itr_path = p.begin();
        std::filesystem::path::const_iterator itr_relative_to = r.begin();
        while(*itr_path == *itr_relative_to && itr_path != p.end() && itr_relative_to != r.end()) {
            ++itr_path;
            ++itr_relative_to;
        }

        // add "../" for each remaining token in relative_to
        if(itr_relative_to != r.end()) {
            ++itr_relative_to;
            while(itr_relative_to != r.end()) {
                result /= "..";
                ++itr_relative_to;
            }
        }

        // add remaining path
        while(itr_path != p.end()) {
            result /= *itr_path;
            ++itr_path;
        }

        return result;
    }

	void WriteNative(const std::string& filename, std::unordered_map<std::string, std::unique_ptr<std::vector<ProfileInfo>>>& mergedProfileInfo)
	{
		std::ofstream ofs(filename, std::ofstream::out);

		for (auto& it : lineData)
		{
            // Replace by relative path if code path is fully include inside
            std::string fileName = it.first;
            std::string sourceLower = RuntimeOptions::Instance().CodePath;
            std::transform(sourceLower.begin(), sourceLower.end(), sourceLower.begin(), ::tolower);

            if(!RuntimeOptions::Instance().Exclude.empty() && fileName.find(RuntimeOptions::Instance().Exclude) != std::string::npos)
            {
                continue;
            }

            // When relative system ...
            if(RuntimeOptions::Instance().Relative)
            {
                // ... search part of path (in lower case or normal)
                if(fileName.find(sourceLower) != std::string::npos)
                    fileName = relativePath(fileName, sourceLower).string();
                else if(fileName.find(RuntimeOptions::Instance().CodePath) != std::string::npos)
                    fileName = relativePath(fileName, RuntimeOptions::Instance().CodePath).string();
            }

			ofs << "FILE: " << fileName << std::endl;
			auto ptr = it.second.get();

			std::string result;
			result.reserve(ptr->numberLines + 1);

			for (size_t i = 0; i < ptr->numberLines; ++i)
			{
				char state = 'i';
				if (ptr->relevant[i])
				{
					auto& line = ptr->lines[i];
					if (line.DebugCount == 0)
					{
						state = '_';
					}
					else if (line.DebugCount == line.HitCount)
					{
						state = 'c';
					}
					else if (line.HitCount == 0)
					{
						state = 'u';
					}
					else
					{
						state = 'p';
					}
				}
				result.push_back(state);
			}

			ofs << "RES: " << result << std::endl;

			auto profInfo = mergedProfileInfo.find(it.first);

			if (profInfo == mergedProfileInfo.end())
			{
				ofs << "PROF: " << std::endl;
			}
			else
			{
				ofs << "PROF: ";
				for (auto& it : *(profInfo->second.get()))
				{
					ofs << int(it.Deep) << ',' << int(it.Shallow) << ',';
				}
				ofs << std::endl;
			}
		}

        ofs.close();

        // Check file exists after write
        if (!std::filesystem::exists(filename))
        {
            const std::string msg = "ERROR: Coverage file hasn't been write on disk at " + filename;
            throw std::exception(msg.c_str());
        }   
	}
};
