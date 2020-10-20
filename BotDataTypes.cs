using System;
using System.IO;

namespace Stress_checker{
    class ApplicationData{

        ///
        /// <summary>
        ///     Telegram bot token of this application
        /// </summary>
        public string telegram_token { get; set; }
        ///
        /// <summary>
        ///     Connection string to connect application to PostgreSQL server
        /// </summary>
        public string db_connection_string { get; set; }
        ///
        /// <summary>
        ///     File where all stresses for database restore is stored
        /// </summary>
        public string stresses_file { get; set; } = Path.Combine(Environment.CurrentDirectory, "stresses.txt");
        ///
        /// <summary>
        ///     Password used to register in telegram chat
        /// </summary>
        public string invite_code { get; set; }
        ///
        /// <summary>
        ///     Property that makes force restore for database
        /// </summary>
        public bool forceRestore { get; set; }
        ///
        /// <summary>
        ///     List of application's administrators
        /// </summary>
        public uint[] admins { get; set; }
        public ApplicationData() {}
    }
    
    ///
    /// <summary>
    ///     Instance of one record in table with words ( in database )
    /// </summary>
    class Words{
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