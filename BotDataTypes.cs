using System;
using System.IO;

namespace Stress_checker{
    class ApplicationData{
        public string telegram_token { get; set; } // telegram bot token
        public string db_connection_string { get; set; } // connection string for database ( PostgreSQL )
        public string stresses_file { get; set; } = Path.Combine(Environment.CurrentDirectory, "stresses.txt"); // file where all stresses for database restore is stored
        public string invite_code { get; set; } // register password
        public bool forceRestore { get; set; } // to rewrite all data in database
        public bool debug { get; set; } = false;
        private string _logging;
        public string logging {   // directory where loggs will be stored
                get { return _logging; } 
                set{
                    if( value[0] == '/' || value[0] == '~' ) _logging = value;
                    else _logging = Path.Combine(Environment.CurrentDirectory, value);
                } 
            }
        public ApplicationData() {}
    }
    
    class Words{ // class that shows one record in table with words ( in database )
        public int? id { get; set; }
        public string word { get; set; }
        public string definition { get; set; }

        public Words() {}
        public Words(string word, bool splitable = false){
            if(splitable == false){
                this.word = word;
            }else{
                string[] peaces = word.Split(" ");
                this.word = peaces[0];

                for(var i = 1; i < peaces.Length; i++){
                    if(i!=1)
                        this.definition+=" ";
                    this.definition+=peaces[i];
                }
                this.definition = this.definition.Substring(1,this.definition.Length - 2);
            }
        }
    }
}