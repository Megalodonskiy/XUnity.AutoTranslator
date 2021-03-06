﻿using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using XUnity.AutoTranslator.Plugin.Core.Configuration;
using XUnity.AutoTranslator.Plugin.Core.Extensions;

namespace XUnity.AutoTranslator.Plugin.Core.Parsing
{

   internal class RichTextParser : ITextParser
   {
      private static readonly char[] TagNameEnders = new char[] { '=', ' ' };
      private static readonly Regex TagRegex = new Regex( "<.*?>" );
      private static readonly HashSet<string> IgnoreTags = new HashSet<string> { "ruby", "group" };
      private static readonly HashSet<string> KnownTags = new HashSet<string> { "b", "i", "size", "color", "ruby", "em", "sup", "sub", "dash", "space", "group", "u", "strike", "param", "format", "emoji", "speed", "sound", "line-height" };

      public RichTextParser()
      {

      }

      public bool CanApply( object ui )
      {
         return Settings.HandleRichText && ui.SupportsRichText();
      }

      private static bool IsAllLatin( string value, int endIdx )
      {
         for( int j = 0; j < endIdx; j++ )
         {
            var c = value[ j ];
            if( !( ( c >= '\u0041' && c <= '\u005a' ) || ( c >= '\u0061' && c <= '\u007a' ) || c == '-' || c == '_' ) )
            {
               return false;
            }
         }
         return true;
      }

      private static bool StartsWithPound( string value, int endIdx )
      {
         return 0 < value.Length && value[ 0 ] == '#';
      }


      public ParserResult Parse( string input, int scope )
      {
         if( !Settings.HandleRichText ) return null;

         var matches = TagRegex.Matches( input );

         var accumulation = new StringBuilder();
         var args = new Dictionary<string, string>();
         var template = new StringBuilder( input.Length );
         var offset = 0;
         var arg = 'A';
         var foundIgnoredTag = false;

         foreach( Match m in matches )
         {
            var tag = m.Value;
            var value = tag.Substring( 1, tag.Length - 2 );
            bool isEndTag = value.StartsWith( "/" );
            if( isEndTag )
            {
               value = value.Substring( 1, value.Length - 1 );
            }

            var parts = value.Split( TagNameEnders );
            if( parts.Length >= 2 )
            {
               value = parts[ 0 ];
            }
            var isKnown = KnownTags.Contains( value );
            var isIgnored = IgnoreTags.Contains( value );
            if( !isKnown )
            {
               var endIdx = value.Length;
               isKnown = IsAllLatin( value, endIdx ) || StartsWithPound( value, endIdx );
            }

            // add normal text
            var end = m.Index;
            var start = offset;
            var text = input.Substring( start, end - start );
            offset = end + m.Length;

            // if the tag is not known, we want to include as normal text in the NEXT iteration
            if( !isKnown )
            {
               accumulation.Append( text );
               accumulation.Append( m.Value );
            }
            else
            {
               text += accumulation;
               accumulation.Length = 0;

               if( !string.IsNullOrEmpty( text ) )
               {
                  var argument = "[[" + ( arg++ ) + "]]";
                  args.Add( argument, text );
                  template.Append( argument );
               }

               if( !isIgnored )
               {
                  template.Append( m.Value );
               }
               else
               {
                  foundIgnoredTag = true;
               }
            }
         }

         // catch any remaining text
         if( offset < input.Length )
         {
            var argument = "[[" + ( arg++ ) + "]]";
            var text = input.Substring( offset, input.Length - offset );
            args.Add( argument, text );
            template.Append( argument );
         }


         var templateString = template.ToString();
         int idx = -1;
         while( ( idx = templateString.IndexOf( "]][[" ) ) != -1 )
         {
            var arg1 = templateString[ idx - 1 ];
            var arg2 = templateString[ idx + 4 ];

            var key1 = "[[" + arg1 + "]]";
            var key2 = "[[" + arg2 + "]]";

            var text1 = args[ key1 ];
            var text2 = args[ key2 ];

            var fullText = text1 + text2;
            var fullKey = key1 + key2;
            var newKey = "[[" + ( arg1 ) + "]]";

            args.Remove( key1 );
            args.Remove( key2 );
            args.Add( newKey, fullText );
            templateString = templateString.Replace( fullKey, newKey );
         }

         var success = foundIgnoredTag || ( arg != 'A' && templateString.Length > 5 );
         if( !success ) return null;

         return new ParserResult( ParserResultOrigin.RichTextParser, input, templateString, false, true, true, false, args );
      }
   }
}
