﻿// Copyright © 2012 onwards, Andrew Whewell
// All rights reserved.
//
// Redistribution and use of this software in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//    * Neither the name of the author nor the names of the program's contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS OF THE SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualRadar.Interface
{
    /// <summary>
    /// The interface for objects that can cache the filenames in a folder.
    /// </summary>
    /// <remarks><para>
    /// This was written to cache the names of aircraft pictures in an aircraft picture folder. It was found that
    /// when the picture folder held several thousand aircraft the lookup times could be painful, so the program
    /// now uses this to cache the filenames in the folder.
    /// </para><para>
    /// The .NET FileSystemWatcher object was tested but was found to be unreliable when the aircraft picture folder
    /// was a network share, particularly when the network share was hosted on a non-Windows machine. So instead
    /// the implementation may employ a polling technique to fetch files. This can introduce the following problems:
    /// </para><list type="bullet">
    ///     <item><description>New files may be reported as missing when they exist.</description></item>
    ///     <item><description>Deleted files may be reported as existing when they don't.</description></item>
    /// </list><para>
    /// This is not a particular problem for the intended use of the cache as users are unlikely to delete aircraft
    /// pictures once they have them and missing a new file isn't the end of the world either. Making these kinds
    /// of mistakes will only lead to the possibility of very occasional website picture glitches. However it can
    /// make this cache unsuitable for more critical purposes.
    /// </para><para>
    /// The cache can detect the following changes between polls:
    /// </para><list type="bullet">
    ///     <item><description>Files that have been added.</description></item>
    ///     <item><description>Files that have been removed.</description></item>
    ///     <item><description>Files that have been renamed*.</description></item>
    ///     <item><description>Files whose last modified time has changed.</description></item>
    /// </list><para>
    /// It will not pick up changes in sub-folders or changes to size, create time, access time or case. If the file is renamed
    /// but a new file matches the name of a file in the cache, and the renamed file has the same modified time as the cached
    /// file, and the old file is replaced with a new file that has the same modified time as the old file, then the rename
    /// will not be detected.
    /// </para></remarks>
    public interface IDirectoryCache
    {
        /// <summary>
        /// Gets or sets the object that abstracts away the environment for testing.
        /// </summary>
        IDirectoryCacheProvider Provider { get; set; }

        /// <summary>
        /// Gets or sets the folder whose content should be cached. Setting a new folder name erases the current
        /// cache and forces the caching on a background thread of the filesnames in the new folder.
        /// </summary>
        string Folder { get; set; }

        /// <summary>
        /// Raised on a background after the cache has been modified in some way. It is not raised if the content of
        /// the cache did not change.
        /// </summary>
        /// <remarks>
        /// In particular if a background poll finds no changes to the cache then it does not raise the event. Setting
        /// <see cref="Folder"/> or calling <see cref="Add"/> or <see cref="Remove"/> can raise this, but only if it
        /// triggered some change in the cache.
        /// </remarks>
        event EventHandler CacheChanged;

        /// <summary>
        /// Takes the full path to the file and returns true if the file is in the cache.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <remarks>
        /// If the file name does not start with <see cref="Folder"/> (case insensitive) then false is always
        /// returned. This method may block if the cache is currently being filled on a background thread or another
        /// thread is interrogating the cache. Passing null or an empty string always returns false.
        /// </remarks>
        bool FileExists(string fileName);

        /// <summary>
        /// Takes the full path to the file and adds it to the cache if it is not already in there.
        /// </summary>
        /// <param name="fileName"></param>
        /// <remarks>
        /// Intended for use by plugins that want to force an immediate load of the file into the cache. The function
        /// will not return until the filename is in the cache. It may block if the cache is being modified / interrogated
        /// on another thread. If the file already exists then the function does nothing. If null or an empty string
        /// is passed then it does nothing. If the file does not start with <see cref="Folder"/> (case-insensitive)
        /// then it does nothing.
        /// </remarks>
        void Add(string fileName);

        /// <summary>
        /// Takes the full path to the file and removes it from the cache if it exists.
        /// </summary>
        /// <param name="fileName"></param>
        /// <remarks>
        /// Intended for use by plugins that want to force an immediate removal of the file from the cache. The function
        /// will not return until the filename has been removed from the cache. It may block if the cache is being modified
        /// / interrogated on another thread. If the file is not in the cache then the function does nothing. If null or an
        /// empty string is passed then it does nothing. If the file does not start with <see cref="Folder"/>
        /// (case-insensitive) then it does nothing.
        /// </remarks>
        void Remove(string fileName);

        /// <summary>
        /// Starts an immediate refresh of the cache.
        /// </summary>
        /// <remarks>
        /// Intended for occasions where a user might want to manually start a reload of the filename cache. May block if the
        /// cache is already being refreshed until the first refresh has completed, at which point it will queue a new refresh
        /// and return.
        /// </remarks>
        void BeginRefresh();
    }
}
