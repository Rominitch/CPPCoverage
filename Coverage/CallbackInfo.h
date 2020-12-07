#pragma once

#include "ProcessInfo.h"
#include "Disassembler/ReachabilityAnalysis.h"

#include <cassert>
#include <filesystem>
#include <set>
#include <vector>

struct FileCallbackInfo;

struct CallbackInfo
{
	CallbackInfo(FileCallbackInfo* fileInfo, ProcessInfo* processInfo, bool registerLines) :
		fileInfo(fileInfo),
		processInfo(processInfo),
		registerLines(registerLines)
	{}

	FileCallbackInfo* fileInfo;
	ProcessInfo* processInfo;
	bool registerLines;
	std::set<PVOID> breakpointsToSet;

	std::filesystem::path compiledReplacePath;	///< Path to original PDB (from compilation)
	std::filesystem::path finalReplacePath;		///< Path to original PDB (from compilation)

	std::vector<ReachabilityAnalysis> reachableCode;

	void computeCompiledPath(const std::filesystem::path& compiledPDB, const std::filesystem::path& executedPDB)
	{
		compiledReplacePath = compiledPDB;
		finalReplacePath    = executedPDB;
		
		while(compiledReplacePath.filename() == finalReplacePath.filename())
		{
			// Go to parent
			compiledReplacePath = compiledReplacePath.parent_path();
			finalReplacePath    = finalReplacePath.parent_path();

			// Common path
			if( compiledPDB.root_path() == compiledPDB )
			{
				// Disable feature
				compiledReplacePath.clear();
				finalReplacePath.clear();
				return;
			}
		}
	}

	//Compute new absolute path from compiled path
	std::filesystem::path rebased(const std::filesystem::path& source) const
	{
		if (compiledReplacePath.empty())
			return source;
		else
		{
			const auto relative = std::filesystem::relative(source, compiledReplacePath);
			if( relative.is_relative() && !relative.empty() ) // Check if possible
			{
				auto ret = std::filesystem::canonical(finalReplacePath / relative);
				assert(std::filesystem::is_regular_file(ret));
				return ret;
			}
			else
				return source;
		}
	}

	void SetBreakpoints(PVOID baseAddress, HANDLE process)
	{
		BYTE buffer[4096];

		auto it = breakpointsToSet.begin();
		while (it != breakpointsToSet.end())
		{
			auto jt = breakpointsToSet.lower_bound(reinterpret_cast<BYTE*>(*it) + 4096);
			if (jt == breakpointsToSet.end() || jt == it)
			{
				for (; it != breakpointsToSet.end(); ++it)
				{
					auto addr = *it;

					BYTE instruction;
					SIZE_T readBytes;

					// Read the first instruction    
					ReadProcessMemory(process, addr, &instruction, 1, &readBytes);

					// Save breakpoint data
					processInfo->breakPoints.find(addr)->second.originalData = instruction;

					// Replace it with Breakpoint
					instruction = 0xCC;
					WriteProcessMemory(process, addr, &instruction, 1, &readBytes);

					// Flush to process -- we might want to do this another way
					FlushInstructionCache(process, addr, 1);
				}
			}
			else
			{
				auto diff = reinterpret_cast<BYTE*>(*jt) - reinterpret_cast<BYTE*>(*it);
				if (diff > 4096) { diff = 4096; }

				SIZE_T readBytes;

				auto startAddr = reinterpret_cast<BYTE*>(*it);

				// Read the instructions
				ReadProcessMemory(process, startAddr, buffer, diff, &readBytes);

				auto limit = startAddr + readBytes;

				for (; it != breakpointsToSet.end() && reinterpret_cast<BYTE*>(*it) < limit; ++it)
				{
					auto addr = *it;
					auto idx = reinterpret_cast<BYTE*>(addr) - startAddr;

					// Save breakpoint data
					processInfo->breakPoints.find(addr)->second.originalData = buffer[idx];

					// Replace it with Breakpoint
					buffer[idx] = 0xCC;
				}

				WriteProcessMemory(process, startAddr, buffer, readBytes, &readBytes);

				// Flush to process -- we might want to do this another way
				FlushInstructionCache(process, startAddr, readBytes);
			}
		}

		// Clear for next module that's loaded
		breakpointsToSet.clear();
	}
};
