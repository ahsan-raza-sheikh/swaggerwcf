﻿using System;
using System.Configuration;

namespace SwaggerWcf.Configuration
{
    public class TagElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return (string) this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("visible", DefaultValue = true, IsRequired = true)]
        public bool Visibile
        {
            get
            {
                return (bool) this["visible"];
            }
            set
            {
                this["visible"] = value;
            }
        }

        [ConfigurationProperty("description", DefaultValue = "", IsRequired = false)]
        public string Description {
            get => this["description"] as string;
            set => this["description"] = value;
        }


        [ConfigurationProperty("sortOrder", DefaultValue = "0", IsRequired = false)]
        public int SortOrder 
        {
            get 
            {
                return (int) this["sortOrder"];
            }
            set
            {
                this["sortOrder"] = value;
            }
        }
    }
}
