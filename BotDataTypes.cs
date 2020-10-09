namespace Stress_checker{
    class ApplicationData{
        public string telegram_token { get; set; }
        public string db_connection_string { get; set; }
        public string stresses_file { get; set; } = "stresses.txt";
        public string invite_code { get; set; }
        public bool forceRestore { get; set; }
        public ApplicationData() {}
    }
    
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