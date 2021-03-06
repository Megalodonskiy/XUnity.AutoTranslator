﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace XUnity.Common.Utilities
{
   /// <summary>
   /// WARNING: Pubternal API (internal). Do not use. May change during any update.
   /// </summary>
   public static class ExpressionHelper
   {
      /// <summary>
      /// WARNING: Pubternal API (internal). Do not use. May change during any update.
      /// </summary>
      /// <param name="method"></param>
      /// <returns></returns>
      public static Delegate CreateTypedFastInvoke( MethodInfo method )
      {
         if( method == null ) throw new ArgumentNullException( "method" );
         if( method.IsGenericMethod )  throw new ArgumentException( "The provided method must not be generic.", "method" );

         if( method.IsStatic )
         {
            var parameters = method.GetParameters()
               .Select( p => Expression.Parameter( p.ParameterType, p.Name ) )
               .ToArray();

            var call = Expression.Call( null, method, parameters );

            return Expression.Lambda( call, parameters ).Compile();
         }
         else
         {
            var parameters = method.GetParameters()
               .Select( p => Expression.Parameter( p.ParameterType, p.Name ) )
               .ToList();

            parameters.Insert( 0, Expression.Parameter( method.DeclaringType, "instance" ) );

            var call = Expression.Call( parameters[ 0 ], method, parameters.Skip( 1 ).ToArray() );

            return Expression.Lambda( call, parameters.ToArray() ).Compile();
         }
      }

      /// <summary>
      /// WARNING: Pubternal API (internal). Do not use. May change during any update.
      /// </summary>
      /// <param name="method"></param>
      /// <returns></returns>
      public static Func<object, object[], object> CreateFastInvoke( MethodInfo method )
      {
         var instanceParameterExpression = Expression.Parameter( typeof( object ), "instance" );
         var argumentsParameterExpression = Expression.Parameter( typeof( object[] ), "args" );
         
         var index = 0;
         var argumentExtractionExpressions =
            method
            .GetParameters()
            .Select( parameter =>
                Expression.Convert(
                   Expression.ArrayIndex(
                      argumentsParameterExpression,
                      Expression.Constant( index++ )
                   ),
                   parameter.ParameterType
                )
            ).ToArray();

         var callExpression = method.IsStatic
            ? Expression.Call( method, argumentExtractionExpressions )
            : Expression.Call(
               Expression.Convert(
                  instanceParameterExpression,
                  method.DeclaringType
               ),
               method,
               argumentExtractionExpressions
            );

         var isVoidReturn = method.ReturnType == typeof( void );

         var finalExpression = isVoidReturn
            ? (Expression)callExpression
            : Expression.Convert( callExpression, typeof( object ) );

         if( isVoidReturn )
         {
            var lambdaExpression = Expression.Lambda<Action<object, object[]>>(
               finalExpression,
               instanceParameterExpression,
               argumentsParameterExpression
            );

            var compiledLambda = lambdaExpression.Compile();
            return ConvertToFunc( compiledLambda );
         }
         else
         {
            var lambdaExpression = Expression.Lambda<Func<object, object[], object>>(
               finalExpression,
               instanceParameterExpression,
               argumentsParameterExpression
            );

            var compiledLambda = lambdaExpression.Compile();
            return compiledLambda;
         }
      }

      /// <summary>
      /// WARNING: Pubternal API (internal). Do not use. May change during any update.
      /// </summary>
      /// <param name="field"></param>
      /// <returns></returns>
      public static Func<object, object> CreateFastInvoke( FieldInfo field )
      {
         var instanceParameterExpression = Expression.Parameter( typeof( object ), "instance" );

         var fieldExpression = field.IsStatic
            ? Expression.Field( null, field )
            : Expression.Field(
               Expression.Convert(
                  instanceParameterExpression,
                  field.DeclaringType
               ),
               field
            );

         var finalExpression = Expression.Convert( fieldExpression, typeof( object ) );

         var lambdaExpression = Expression.Lambda<Func<object, object>>(
            finalExpression,
            instanceParameterExpression
         );

         var compiledLambda = lambdaExpression.Compile();
         return compiledLambda;
      }

      private static Func<object, object[], object> ConvertToFunc( Action<object, object[]> action )
      {
         return ( instance, args ) =>
         {
            action( instance, args );
            return null;
         };
      }
   }
}
