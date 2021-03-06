﻿using System;

namespace Tests.TestCases
{
    public class StaticFieldVisible
    {
        public static double Pi = 3.14;  // Noncompliant
//                           ^^
        public const double Pi2 = 3.14;
        public double Pi3 = 3.14;

        [ThreadStatic]
        public static int value; // Noncompliant, FP - thread static field values are not shared between threads. See https://github.com/SonarSource/sonar-dotnet/issues/4407
    }

    public class Shape
    {
        public static Shape Empty = new EmptyShape(); // Noncompliant {{Change the visibility of 'Empty' or make it 'const' or 'readonly'.}}
        public static readonly Shape Empty2 = new EmptyShape();

        private class EmptyShape : Shape
        {
        }
    }
}
