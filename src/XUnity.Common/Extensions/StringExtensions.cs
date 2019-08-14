﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace XUnity.Common.Extensions
{
   public static class StringExtensions
   {

      private static readonly HashSet<char> InvalidFileNameChars = new HashSet<char>( Path.GetInvalidFileNameChars() );
      private static readonly string[] NewlinesCharacters = new string[] { "\r\n", "\n" };
      private static readonly char[] WhitespacesAndNewlines = new char[] { '\r', '\n', ' ', '　' };
      private static readonly char[] Spaces = new char[] { ' ', '　' };

      public static string MakeRelativePath( this string fullOrRelativePath, string basePath )
      {
         var builder = new StringBuilder();
         int offset = 0;
         bool impossible = false;

         //// this is the easy case.  The file is inside of the working directory.
         //if( fullPath.StartsWith( basePath ) )
         //{
         //   return fullPath.Substring( basePath.Length + 1 );
         //}

         // the hard case has to back out of the working directory
         string[] baseDirs = basePath.Split( new char[] { ':', '\\', '/' } );
         var fileDirs = fullOrRelativePath.Split( new char[] { ':', '\\', '/' } ).ToList();

         // if we failed to split (empty strings?) or the drive letter does not match
         if( baseDirs.Length <= 0 || fileDirs.Count <= 0 || baseDirs[ 0 ] != fileDirs[ 0 ] )
         {
            // can't create a relative path between separate harddrives/partitions.
            impossible = true;
         }

         if( !impossible )
         {
            // skip all leading directories that match
            for( offset = 1; offset < baseDirs.Length; offset++ )
            {
               if( baseDirs[ offset ] != fileDirs[ offset ] )
                  break;
            }

            // back out of the working directory
            for( int i = 0; i < ( baseDirs.Length - offset ); i++ )
            {
               builder.Append( "..\\" );
            }
         }

         for( int i = offset; i < fileDirs.Count; i++ )
         {
            if( fileDirs[ i ] == ".." )
            {
               var previousIndex = i - 1;
               if( previousIndex >= offset )
               {
                  fileDirs.RemoveAt( i );
                  fileDirs.RemoveAt( previousIndex );
                  i -= 2;
               }
            }
         }

         // step into the file path
         for( int i = offset; i < fileDirs.Count - 1; i++ )
         {
            var dir = fileDirs[ i ];
            if( dir != null )
            {
               builder.Append( dir )
                  .Append( '\\' );
            }
         }

         var lastIndex = fileDirs.Count - 1;
         var lastDir = fileDirs[ lastIndex ];
         if( lastDir != null )
         {
            builder.Append( lastDir );
         }

         return builder.ToString();
      }

      public static string SanitizeForFileSystem( this string path )
      {
         var builder = new StringBuilder( path.Length );
         foreach( var c in path )
         {
            if( !InvalidFileNameChars.Contains( c ) )
            {
               builder.Append( c );
            }
         }
         return builder.ToString();
      }

      public static string SplitToLines( this string text, int maxStringLength, params char[] splitOnCharacters )
      {
         var sb = new StringBuilder();
         var index = 0;

         while( text.Length > index )
         {
            // start a new line, unless we've just started
            if( index != 0 )
               sb.Append( '\n' );

            // get the next substring, else the rest of the string if remainder is shorter than `maxStringLength`
            var splitAt = index + maxStringLength <= text.Length
                ? text.Substring( index, maxStringLength ).LastIndexOfAny( splitOnCharacters )
                : text.Length - index;

            // if can't find split location, take `maxStringLength` characters
            splitAt = ( splitAt == -1 ) ? maxStringLength : splitAt;

            // add result to collection & increment index
            sb.Append( text.Substring( index, splitAt ).Trim() );
            index += splitAt;
         }

         return sb.ToString();
      }

      public static bool StartsWithStrict( this string str, string prefix )
      {
         var len = Math.Min( str.Length, prefix.Length );
         if( len < prefix.Length ) return false;

         for( int i = 0; i < len; i++ )
         {
            if( str[ i ] != prefix[ i ] ) return false;
         }

         return true;
      }

      public static string GetBetween( this string strSource, string strStart, string strEnd )
      {
         const int kNotFound = -1;

         var startIdx = strSource.IndexOf( strStart );
         if( startIdx != kNotFound )
         {
            startIdx += strStart.Length;
            var endIdx = strSource.IndexOf( strEnd, startIdx );
            if( endIdx > startIdx )
            {
               return strSource.Substring( startIdx, endIdx - startIdx );
            }
         }
         return string.Empty;
      }

      public static bool RemindsOf( this string that, string other )
      {
         return that.StartsWith( other ) || other.StartsWith( that ) || that.EndsWith( other ) || other.EndsWith( that );
      }
   }
}