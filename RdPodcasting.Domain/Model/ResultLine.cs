﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RdPodcasting.Domain.Model
{
    public class ResultLine
    {
        public ResultLine() { }

        public string Estado { get; set; }
        public int Casos { get; set; }
        public string Mortos { get; set; }

    }
}