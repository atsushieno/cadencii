/*
 * BProgressBar.cs
 * Copyright (c) 2009 kbinani
 *
 * This file is part of bocoree.
 *
 * bocoree is free software; you can redistribute it and/or
 * modify it under the terms of the BSD License.
 *
 * bocoree is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 */
#if JAVA
//INCLUDE ..\BuildJavaUI\src\org\kbinani\windows\forms\BProgressBar.java
#else
namespace bocoree.windows.forms {

    public class BProgressBar : System.Windows.Forms.ProgressBar {
        public int getMaximum() {
            return base.Maximum;
        }

        public void setMaximum( int value ) {
            base.Maximum = value;
        }

        public int getMinimum() {
            return base.Minimum;
        }

        public void setMinimum( int value ) {
            base.Minimum = value;
        }

        public int getValue() {
            return base.Value;
        }

        public void setValue( int value ) {
            base.Value = value;
        }
    }

}
#endif
