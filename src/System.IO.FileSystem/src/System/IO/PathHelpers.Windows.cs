// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO
{
    internal static partial class PathHelpers
    {

        internal static bool ShouldReviseDirectoryPathToCurrent(string path)
        {
            // In situations where this method is invoked, "<DriveLetter>:" should be special-cased 
            // to instead go to the current directory.
            return path.Length == 2 && path[1] == ':';
        }

        // ".." can only be used if it is specified as a part of a valid File/Directory name. We disallow
        //  the user being able to use it to move up directories. Here are some examples eg 
        //    Valid: a..b  abc..d
        //    Invalid: ..ab   ab..  ..   abc..d\abc..
        //
        internal static void CheckSearchPattern(string searchPattern)
        {
            for (int index = 0; (index = searchPattern.IndexOf("..", index, StringComparison.Ordinal)) != -1; index += 2)
            {
                // Terminal ".." or "..\". File and directory names cannot end in "..".
                if (index + 2 == searchPattern.Length || 
                    PathInternal.IsDirectorySeparator(searchPattern[index + 2]))
                {
                    throw new ArgumentException(SR.Arg_InvalidSearchPattern, nameof(searchPattern));
                }
            }
        }

        // this is a lightweight version of GetDirectoryName that doesn't renormalize
        internal static string GetDirectoryNameInternal(string path)
        {
            string directory, file;
            SplitDirectoryFile(path, out directory, out file);

            // file is null when we reach the root
            return (file == null) ? null : directory;
        }

        internal static void SplitDirectoryFile(string path, out string directory, out string file)
        {
            directory = null;
            file = null;

            // assumes a validated full path
            if (path != null)
            {
                int length = path.Length;
                int rootLength = PathInternal.GetRootLength(path);

                // ignore a trailing slash
                if (length > rootLength && EndsInDirectorySeparator(path))
                    length--;

                // find the pivot index between end of string and root
                for (int pivot = length - 1; pivot >= rootLength; pivot--)
                {
                    if (PathInternal.IsDirectorySeparator(path[pivot]))
                    {
                        directory = path.Substring(0, pivot);
                        file = path.Substring(pivot + 1, length - pivot - 1);
                        return;
                    }
                }

                // no pivot, return just the trimmed directory
                directory = path.Substring(0, length);
            }
        }

        internal static string NormalizeSearchPattern(string searchPattern)
        {
            Debug.Assert(searchPattern != null);

            // Win32 normalization trims only U+0020.
            string tempSearchPattern = searchPattern.TrimEnd(PathHelpers.TrimEndChars);

            // Make this corner case more useful, like dir
            if (tempSearchPattern.Equals("."))
            {
                tempSearchPattern = "*";
            }

            CheckSearchPattern(tempSearchPattern);
            return tempSearchPattern;
        }

        internal static string GetFullSearchString(string fullPath, string searchPattern)
        {
            Debug.Assert(fullPath != null);
            Debug.Assert(searchPattern != null);

            ThrowIfEmptyOrRootedPath(searchPattern);
            string tempStr = Path.Combine(fullPath, searchPattern);

            // If path ends in a trailing slash (\), append a * or we'll get a "Cannot find the file specified" exception
            char lastChar = tempStr[tempStr.Length - 1];
            if (PathInternal.IsDirectorySeparator(lastChar) || lastChar == Path.VolumeSeparatorChar)
            {
                tempStr = tempStr + "*";
            }

            return tempStr;
        }

        internal static string TrimEndingDirectorySeparator(string path) =>
            EndsInDirectorySeparator(path) ?
                path.Substring(0, path.Length - 1) :
                path;
    }
}
