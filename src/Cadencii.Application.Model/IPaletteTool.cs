/*
 * IPaletteTool.cs
 * Copyright © 2009-2011 kbinani
 *
 * This file is part of cadencii.
 *
 * cadencii is free software; you can redistribute it and/or
 * modify it under the terms of the GPLv3 License.
 *
 * cadencii is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 */
using System;
using System.Collections.Generic;

using cadencii.vsq;
using cadencii.java.awt;



namespace cadencii
{

    public interface IPaletteTool
    {
        bool edit(VsqTrack track, int[] event_internal_ids, MouseButtons button);
        string getName(string language);
        string getDescription(string language);
        bool hasDialog();
        DialogResult openDialog();
        Image getIcon();
        void applyLanguage(string language);
    }

}