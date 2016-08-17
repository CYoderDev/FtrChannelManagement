﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.NGVODPoster
{
    public class VODAsset
    {
        public int AssetId { get; set; }
        public string Title { get; set; }
        public string PID { get; set; }
        public string PAID { get; set; }
        public string PosterSource { get; set; }
        public string PosterDest { get; set; }
        public List<VODFolder> Folders { get; set; }

        public VODAsset()
        {
            this.Folders = new List<VODFolder>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Asset ID: {0}{4}Title: {1}{4}PID: {2}{4}PAID: {3}{4}Source File: {5}", this.AssetId, this.Title, this.PID, this.PAID, System.Environment.NewLine, this.PosterSource);
            return sb.ToString();
        }
    }
}
