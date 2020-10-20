
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using System.Text.Json;
using System.Threading;
using Telegram.Bot;
using System.Linq;
using System.IO;
using Npgsql;
using System;
using System.Net;

namespace Stress_checker
{
    class StressChecker{
        ApplicationData applicationData;
        TelegramBotClient botClient;
        NpgsqlConnection DBConnection;
        readonly char[] vowels = new char[] {'а', 'е', 'и', 'і', 'о', 'у', 'я', 'ю', 'є', 'ї'};
        public StressChecker(string filename = "config.json"){
            using ( var stream = File.OpenRead(@$"{filename}") ){
                applicationData = JsonSerializer.DeserializeAsync<ApplicationData>(stream).Result;
            }

            DBConnection  = new NpgsqlConnection(applicationData.db_connection_string);
            botClient = new Telegram.Bot.TelegramBotClient(applicationData.telegram_token);
            // stresseRestore();
        }
        public async Task Start(){
            DBConnection.Open();

            botClient.OnMessage += onMessageSend;
            botClient.OnMessageEdited += onMessageChange;
            botClient.StartReceiving();

            Console.WriteLine("Bot has started");

            await Task.Delay(Timeout.Infinite);
            DBConnection.Close();
        }

        async void onMessageSend(object sender, MessageEventArgs e){
            if(e.Message.Text != null && !(await in_proggress(e.Message.From.Id))){
                var message = e.Message.Text;
                if(message[0] == '/'){
                    switch(message.Substring(1).Contains(" ") ? message.Substring(1).Split(" ")[0] : message.Substring(1)){
                        case "start":
                        case "help":
                            await StartMessage(e.Message.Chat.Id);
                            break;
                        case "register":
                            var error = registerUser(e.Message.From.Id, e.Message.Text.Contains(" ") ? e.Message.Text.Split(" ")[1] : "");
                            if(error != null){
                                await botClient.SendTextMessageAsync(e.Message.Chat.Id, error.Message);
                            }
                            break;
                        case "is_registered":
                            await isRegistered(e.Message.From.Id, e.Message.Chat.Id);
                            break;
                        case "start_game":
                            await Game(e.Message.From.Id, e.Message.Chat.Id, message.Substring(1).Contains(" ") ? Convert.ToUInt16(message.Substring(1).Split(" ")[1]) : (ushort)20);
                            break;
                        case "in_game":
                            await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Ні, ви не в грі");
                            break;
                        case "update_data":
                            bool res = appDataUpdate();
                            if(res)
                                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Успішно оновдено данні");
                            else
                                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Не вийшло оновити данні");
                            break;
                        case "my_id":
                            await botClient.SendTextMessageAsync(e.Message.From.Id, $"Ваш ID: {e.Message.From.Id}");
                            break;
                        case "search":
                            if(message.Substring(1).Contains(" "))
                                await searchForWord(chat_id: e.Message.Chat.Id, words: message.Split(" ").Where((e, id) => id != 0).ToArray<string>());
                            break;
                        case "is_admin":
                            await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"{applicationData.admins.Contains((uint)e.Message.From.Id)}");
                            break;
                        case "add":
                            var resu = AddWordToDB(e.Message.Chat.Id, message.Split(" ").Where((e, id) => id != 0).ToList());
                            break;
                        default:
                            await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Незрозуміла команда");
                            break;
                    }
                }else{
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Це так не працює, напиши /help для додаткової інформації", replyToMessageId: e.Message.MessageId);
                }

            }else if(e.Message.Text != null){
                var message = e.Message.Text;
                if(message[0] == '/'){
                    switch(message.Substring(1).Contains(" ") ? message.Substring(1).Split(" ")[0] : message.Substring(1)){
                        case "help":
                            await StartMessage(e.Message.Chat.Id);
                            break;
                        case "quit":
                            await QuitTheGame(e.Message.From.Id, e.Message.Chat.Id);
                            break;
                        case "in_game":
                            await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Так, ви в грі");
                            break;
                        default:
                            await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Ви в грі");
                            break;
                    }
                }if(e.Message.Text == "Готово"){
                    await Game(e.Message.From.Id, e.Message.Chat.Id, nextWord: true);
                }else if(!e.Message.Text.Contains("/") && !e.Message.Text.Contains(" ")){
                    using (var cmd = new NpgsqlCommand($"update users set answers = array_append(answers, '{e.Message.Text}'), ingame = true where userid = @p", DBConnection)){
                        cmd.Parameters.AddWithValue("p", e.Message.From.Id);
                        cmd.ExecuteNonQueryAsync().Wait();
                        await cmd.DisposeAsync();
                    }
                }
            }else{

                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Я сприймаю тільки текст, можливо в майбутньому це зміниться, напиши /help для додаткової інформації");

            }
        }
        async void onMessageChange(object sender, MessageEventArgs e){
            if(e.Message.Text != null){
                int violation = 1;
                await using (var cmd = new NpgsqlCommand($"SELECT violation FROM users WHERE userid="+e.Message.From.Id.ToString(), DBConnection))
                await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync()){
                    violation += reader.GetInt32(0);
                }
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"Changing text isn't allowed. If you change any text you entered, you will have {3-violation} warnings and then you will get ban\n2 attempts left");
                using (var cmd = new NpgsqlCommand("update words set violation = @v where userid = {e.Message.From.Id}", DBConnection)){
                    cmd.Parameters.AddWithValue("v", violation);
                    cmd.ExecuteNonQueryAsync().Wait();
                }
            }
        }
        async Task StartMessage(long chat_id){
            await botClient.SendTextMessageAsync(chat_id, @"Це бот для перевірки наголосів. Для того, щоб це зробити, вам потрібно:
1)  Зареєструватися за допомогою
/register <код запрошення>

    для перевірки, чи ви вже зереєстровані
/is_registered

2)  Власне почати гру
/start_game <кількість слів ( за промовчкванням 20 )>

    для того, щоб вийти з гри ( працює тільки підчас гри)
/quit

    для того, щоб перевірити чи ви зараз в грі
/in_game

3)  Якщо ви пам'ятаєте, як потрібно наголошувати слово:(Ще у розробці)
/search слово

4)  Debug commands
/my_id
/update_data
/is_admin

5)  Для адміністрації
/add_word <слово/слова через пропуск>

-)  Довідка по використанню бота
/help
");
        }
        async Task<bool> in_proggress(int user_id){
            try{
                while(DBConnection.State == System.Data.ConnectionState.Executing){ await Task.Delay(500); }
                if(DBConnection.State == System.Data.ConnectionState.Open)
                await using (var cmd = new NpgsqlCommand($"SELECT ingame FROM users WHERE userid = {user_id};", DBConnection)){
                    await using (var reader = await cmd.ExecuteReaderAsync()){
                        while (await reader.ReadAsync()){
                            return reader.GetFieldValue<bool>(0);
                        }
                    }
                    cmd.Dispose();
                }
                
            }catch(Npgsql.NpgsqlOperationInProgressException e){
                Console.WriteLine($"{DateTime.Now.ToString("[dd/MM/yyyy][HH:mm]")}[in_progress][{e.Data}]: {e.Message}");
                await Task.Delay(500);
                return await in_proggress(user_id);
            }catch(System.InvalidOperationException e){
                Console.WriteLine(1);
                Console.WriteLine($"{DateTime.Now.ToString("[dd/MM/yyyy][HH:mm]")}[in_progress][{e.Data}]: {e.Message}");
                await in_proggress(user_id);
            }catch(System.IO.EndOfStreamException){
                return true;
            }
            return false;
        }
        Exception registerUser(int user_id, string key, bool inProggressError = false){
            if(inProggressError && (applicationData.invite_code == null || key != applicationData.invite_code)){
                return applicationData.invite_code == null ? new Exception("Пусте поле коду запрошення") : new Exception("Не правильне поле коду запрошення");
            }
            if(inProggressError && isRegistered(user_id).Result){
                return new Exception("Ви вже зареєстровані");
            }
            try{
                using (var cmd = new NpgsqlCommand("insert into users (userid) values (@v);", DBConnection)){
                    cmd.Parameters.AddWithValue("v", user_id);
                    cmd.ExecuteNonQueryAsync().Wait();
                    cmd.Dispose();
                }
            }catch(System.AggregateException e){
                Console.WriteLine($"{DateTime.Now.ToString("[dd/MM/yyyy][HH:mm]")}[registerUser][{e.Data}]: {e.Message}");
                return new Exception("Ви вже зареєстровані");
            }catch(Npgsql.NpgsqlOperationInProgressException e){
                Console.WriteLine($"{DateTime.Now.ToString("[dd/MM/yyyy][HH:mm]")}[registerUser][{e.Data}]: {e.Message}");
                return registerUser(user_id, key, true);
            }

            return null;
        }
        async Task<bool> isRegistered(int user_id, long? chat_id = null){
            try{
                await using (var cmd = new NpgsqlCommand($"SELECT id FROM users WHERE userid="+user_id, DBConnection))
                await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync()){
                    if(chat_id==null){
                        
                        return reader.FieldCount != 0;
                    }
                    if(reader.FieldCount != 0){
                        await botClient.SendTextMessageAsync(chat_id, "Ви зареєстровані");
                    }else{
                        await botClient.SendTextMessageAsync(chat_id, "Ви не зареєстровані");
                    }
                    await cmd.DisposeAsync();
                }
            }catch(Npgsql.NpgsqlOperationInProgressException e){
                Console.WriteLine($"{DateTime.Now.ToString("[dd/MM/yyyy][HH:mm]")}[isRegistered][{e.Data}]: {e.Message}");
                return await isRegistered(user_id, chat_id);
            }

            return false;
        }
        bool appDataUpdate(){
            using ( var stream = File.OpenRead("config.json") ){
                applicationData = JsonSerializer.DeserializeAsync<ApplicationData>(stream).Result;
            }

            DBConnection  = new NpgsqlConnection(applicationData.db_connection_string);

            return true;
        }
        bool appDataBackup(){
            string backup = JsonSerializer.Serialize<ApplicationData>(applicationData);
            using (var stream = new StreamWriter("config.json")){
                stream.Write(backup);
            } 
            return true;
        }
        async Task<int> wordsAmount(){
            try{
            using(var comm = new NpgsqlCommand("select count(word) from words;", DBConnection))
            using(var reader = comm.ExecuteReader()){
                while(reader.Read()){
                    return reader.GetInt32(0);
                }
                await comm.DisposeAsync();
            }
            }catch(System.InvalidOperationException){
                await Task.Delay(2000);
                return await wordsAmount();
            }

            return 0;
        }
        async Task<bool> stresseRestore(){

            int wordsAm = await wordsAmount();
            if(applicationData.forceRestore || wordsAm == 0 || wordsAm < File.ReadLines(applicationData.stresses_file).Count()){
                using (var stream = new StreamReader(applicationData.stresses_file)){
                    while(!stream.EndOfStream){
                        string line  = stream.ReadLine();
                        Words words = new Words(line, line.Contains(" "));
                        using (var cmd = new NpgsqlCommand("INSERT INTO words (word, definition) VALUES (@w, @p)", DBConnection)){
                            cmd.Parameters.AddWithValue("w", words.word);
                            cmd.Parameters.AddWithValue("p", words.definition ?? "");
                            cmd.ExecuteNonQueryAsync().Wait();
                            await cmd.DisposeAsync();
                        }
                    }
                }
            }
            return true;
        }

        async Task QuitTheGame(long user_id, long chat_id){
            if(!await in_proggress((int)user_id)){
                await botClient.SendTextMessageAsync(chat_id, "Не вийшло вийти, адже ви не в грі");
                return;
            }

            using( var comm = new NpgsqlCommand("update users set words=null, answers=null, ingame=false where userid=@u", DBConnection)){
                comm.Parameters.AddWithValue("u", user_id);
                await comm.ExecuteNonQueryAsync();
                await comm.DisposeAsync();
            }

            await botClient.SendTextMessageAsync(chat_id, "Вийшло вийти", replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove());
        }

        async Task Game(long user_id, long chat_id, ushort amount = 20, bool nextWord = false){
            List<int> word_array = new List<int>();
            if(!(await in_proggress((int)user_id))){
                while(DBConnection.State == System.Data.ConnectionState.Executing){ await Task.Delay(500); }
                if(DBConnection.State == System.Data.ConnectionState.Open)
                using (var cmd = new NpgsqlCommand($"update users set words = @w, ingame = true where userid = @p", DBConnection)){
                    cmd.Parameters.AddWithValue("w", word_array);
                    cmd.Parameters.AddWithValue("p", user_id);
                    cmd.ExecuteNonQueryAsync().Wait();
                    await cmd.DisposeAsync();
                }
                await using (var cmd = new NpgsqlCommand($"SELECT id FROM words ORDER BY random() LIMIT {amount};", DBConnection))
                await using (var reader = await cmd.ExecuteReaderAsync()){
                    while (await reader.ReadAsync()){
                        word_array.Add(reader.GetInt32(0));
                    }
                }

                using (var cmd = new NpgsqlCommand($"update users set words = @w, ingame = true where userid = @p", DBConnection)){
                    cmd.Parameters.AddWithValue("w", word_array);
                    cmd.Parameters.AddWithValue("p", user_id);
                    cmd.ExecuteNonQueryAsync().Wait();
                    await cmd.DisposeAsync();
                }

            }else{
                await using (var cmd = new NpgsqlCommand($"SELECT words FROM users WHERE userid = {user_id};", DBConnection))
                await using (var reader = await cmd.ExecuteReaderAsync()){
                    while (await reader.ReadAsync()){
                        word_array = reader.GetFieldValue<List<int>>(0);
                    }
                    await cmd.DisposeAsync();
                }
                if(word_array.Count == 0 || word_array == null){
                    await botClient.SendTextMessageAsync(chat_id, "Це все на сьогодні", replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove());
                    await using(var comand = new NpgsqlCommand($"update users set ingame=false where userid={user_id};", DBConnection)){
                        await comand.ExecuteNonQueryAsync();
                        await comand.DisposeAsync();
                        return;
                    }
                }
            }

            Words word = new Words();

            await using (var cmd = new NpgsqlCommand($"SELECT word, definition FROM words WHERE id = {word_array[0]};", DBConnection))
            await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()){
                word.word = reader.GetFieldValue<string>(0);
                word.definition = reader.GetFieldValue<string>(1);
            }

            if(nextWord){
                List<string> answers = null;
                List<string> correct = (List<string>)CorrectOptions(word.word);

                await using (var cmd = new NpgsqlCommand($"SELECT answers FROM users WHERE userid = {user_id};", DBConnection))
                await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync()){
                    try{
                    answers = reader.GetFieldValue<List<string>>(0);
                    if(answers == null || answers.Count == 0)
                        throw new System.InvalidCastException("Null ref exception");
                    }catch(System.InvalidCastException e){
                        Console.WriteLine($"{DateTime.Now.ToString("[dd/MM/yyyy][HH:mm]")}[Game][{e.Data}]: {e.Message}");
                        await botClient.SendTextMessageAsync(chat_id, $"Виших відповідей немає", 
                            replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove());
                        await botClient.SendTextMessageAsync(chat_id, $"Правильні: {string.Join(", ", correct)}");
                        return;
                    }
                }

                if(correct.Count < answers.Count){
                    await botClient.SendTextMessageAsync(chat_id, $"Виших відповідей більше ніж правильних", 
                            replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove());
                    await botClient.SendTextMessageAsync(chat_id, $"Правильні: {string.Join(", ", correct)}");
                }else{
                    int correctAns = 0;
                    foreach(var item in answers){
                        foreach(var jtem in correct){
                            if(item == jtem)
                                correctAns++;
                        }
                    }
                    if(correctAns == correct.Count){
                        await botClient.SendTextMessageAsync(chat_id, $"Чудово, усе правильно");
                    }else{
                        if(correct.Count == 1 || correctAns == 0)
                            await botClient.SendTextMessageAsync(chat_id, "Неправильно");
                        else
                            await botClient.SendTextMessageAsync(chat_id, $"У вас правильних відповідей: {correctAns}/{correct.Count}");
                        await botClient.SendTextMessageAsync(chat_id, $"Правильні: {string.Join(", ", correct)}");
                    }
                }
                using (var comand = new NpgsqlCommand($"UPDATE users set answers=null, words = array_remove(words, {word_array[0]}) where userid={user_id};", DBConnection)){
                    await comand.ExecuteNonQueryAsync();
                }
                await Game(user_id, chat_id, amount);
                return;
            }

            
            string[] options = AllOptions(word.word).ToArray<string>();

            IEnumerable<IEnumerable<Telegram.Bot.Types.ReplyMarkups.KeyboardButton>> keys;
            KeyboardAppend(out keys, options, colSize: 3);
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(keys);
            keyboard.Keyboard = keyboard.Keyboard.Append( new Telegram.Bot.Types.ReplyMarkups.KeyboardButton[]{
                new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Готово")
            });
            
            await botClient.SendTextMessageAsync(chat_id, $"Який наголос правильний?{ (word.definition == "" ? "" : "\nЗначення: " + word.definition) }", replyMarkup: keyboard);
        }
        IEnumerable<string> AllOptions(string word){
            
            List<string> res = new List<string>();

            for(var i = 0;i < word.Length; i++){
                if(vowels.Contains(char.ToLower(word[i]))){
                    var w = new System.Text.StringBuilder(word.ToLower());
                    w = w.Insert(i+1, ((char)769).ToString()); // with ` character
                    // w[i] = char.ToUpper(w[i]); // with big letter insted
                    res.Add(w.ToString());
                }
            }

            return res;
        }
        IEnumerable<string> CorrectOptions(string correctOption){

            List<string> res = new List<string>();

            for(var i = 0;i < correctOption.Length; i++){
                if(char.ToUpper(correctOption[i]) == correctOption[i]){
                    var w = new System.Text.StringBuilder(correctOption.ToLower());
                    w = w.Insert(i+1, ((char)769).ToString()); // with ` character
                    // w[i] = char.ToUpper(w[i]); // with ` character
                    res.Add(w.ToString());
                }
            }

            return res;
        }
        void KeyboardAppend(out IEnumerable<IEnumerable<Telegram.Bot.Types.ReplyMarkups.KeyboardButton>> keyboard, string[] options, int colSize){
            keyboard = new List<List<Telegram.Bot.Types.ReplyMarkups.KeyboardButton>>();
            List<Telegram.Bot.Types.ReplyMarkups.KeyboardButton> row = null;
            for(var i = 0; i < options.Length; i++){
                if(i % colSize == 0){
                    if(i != 0)
                        keyboard = keyboard.Append(row);
                    row = new List<Telegram.Bot.Types.ReplyMarkups.KeyboardButton>();
                }
                row.Add(new Telegram.Bot.Types.ReplyMarkups.KeyboardButton(options[i]));
            }
            keyboard = keyboard.Append(row);
        }

        /// <summary>
        ///     Search for word stress and returns list of it or writes it to chat by chat_id
        /// </summary>
        async Task<List<string>> searchForWord(string[] words, long? chat_id = null){
                // https://goroh.pp.ua/Тлумачення
                // link of site to parse from
                //
                //  Beginning of the useful info:
                //  <div class="list">
                //
                //  End of useful info:
                //  <p class="source-info">

            List<string> stressed_words = new List<string>();
            // string res = "";
            Telegram.Bot.Types.Message message = new Telegram.Bot.Types.Message();
            if(chat_id != null)
                message = await botClient.SendTextMessageAsync(chat_id, "0% зроблено");
            for(var i = 0; i < words.Count(); i++){
                var requets = (HttpWebRequest)WebRequest.Create($"https://goroh.pp.ua/Тлумачення/{words[i]}");
                HttpWebResponse responce;
                try{
                responce = (HttpWebResponse)requets.GetResponse();
                }catch(System.Net.WebException e){
                    Console.WriteLine($"{DateTime.Now.ToString("[dd/MM/yyyy][HH:mm]")}[searchForWord][{e.Data}]: {e.Message}");
                    // message = await botClient.EditMessageTextAsync(chat_id, message.MessageId, $"{message.Text}\n{words[i]} ( Не знайдено слова )");
                    
                    // res+= $"{words[i]} (Не знайдено слова) ";
                    stressed_words.Add($"{words[i]} (Не знайдено слова)");
                    if(chat_id != null && message.Text != $"{ Math.Round((( (float)i + 1f)/ (float)words.Length)*100, 0)}% зроблено")
                        message = await botClient.EditMessageTextAsync(chat_id, message.MessageId, $"{ Math.Round((( (float)i + 1f)/ (float)words.Length)*100, 0)}% зроблено");
                    continue;
                }


                if(responce.StatusCode == HttpStatusCode.OK){

                    var eonc = string.IsNullOrWhiteSpace(responce.CharacterSet);
                    StreamReader stream;

                    if(string.IsNullOrWhiteSpace(responce.CharacterSet))
                        stream = new StreamReader(responce.GetResponseStream());
                    else
                        stream = new StreamReader(responce.GetResponseStream(), System.Text.Encoding.GetEncoding(responce.CharacterSet));

                    string rawData = "";
                    bool firstBegining = true;
                    while(!stream.EndOfStream){
                        string line = await stream.ReadLineAsync();
                        if(line.Contains("<div class=\"list\">") && firstBegining){
                            rawData+=line;
                            firstBegining = !firstBegining;
                        }
                        if(line.Contains("<p class=\"source-info\">")){
                            rawData+=line;
                            break;
                        }

                        if(!firstBegining)
                            rawData+=line;
                    }
                    if(! string.IsNullOrEmpty(rawData)){
                        // res += TextProcessing(rawData).ToLower()+" ";
                        stressed_words.Add(TextProcessing(rawData).ToLower());
                    }

                }
                if(chat_id != null && message.Text != $"{ Math.Round((( (float)i + 1f)/ (float)words.Length)*100, 0)}% зроблено")
                    message = await botClient.EditMessageTextAsync(chat_id, message.MessageId, $"{ Math.Round((( (float)i + 1f)/ (float)words.Length)*100, 0)}% зроблено");        
            }
            if(chat_id != null){
                await botClient.SendTextMessageAsync(chat_id, string.Join(' ', stressed_words));
                await botClient.DeleteMessageAsync(chat_id, message.MessageId);
            }
            return stressed_words;
        }

        /// <summary>
        ///     Getting word from html page got from goroh.pp.com site
        /// </summary>
        string TextProcessing(string rawData){

            // for now only word stress, so beginning is
            // "uppercase">
            // and end is </

            var a = rawData.Split("\"uppercase\">")[1];

            return a.Substring(0, a.IndexOf("</"));
        }

        /// <summary>
        ///      Converting ` character to big letter
        /// </summary>
        void TextProcessing(ref List<string> stressedList){

            for(var i = 0;i < stressedList.Count(); i++){
                var temp = new System.Text.StringBuilder(stressedList[i]);
                for ( var j = temp.Length - 1;j > 0; j-- ){
                    if((int)temp[j] == 769 && j != 0){
                        temp = temp.Remove(j, 1);
                        temp[j-1] = char.ToUpper(temp[j-1]);
                    }
                }
                stressedList[i] = temp.ToString();
            }

        }
        bool AddWordToDB(long chat_id, List<string> rawWords){
            if(!applicationData.admins.Contains((uint)chat_id))
                throw new Exception($"not an admin trying to add {rawWords}");
            
            using(var cmd = new NpgsqlCommand($"SELECT word FROM words WHERE LOWER(word)=LOWER('{ string.Join("') OR LOWER(word)=LOWER(\'", rawWords) }');", DBConnection))
            using(var reader  = cmd.ExecuteReaderAsync().Result){
                while(reader.ReadAsync().Result){
                    var word = rawWords.FirstOrDefault( e => e.ToLower() == reader.GetString(0).ToLower() );
                    rawWords.Remove( word );
                    if(!string.IsNullOrEmpty(word)){
                        botClient.SendTextMessageAsync(chat_id, $"Слово '{word}' вже є в таблиці");
                    }
                }
            }
            if(rawWords.Count == 0){
                return false;
            }

            var searched = searchForWord(rawWords.ToArray()).Result;
            
            rawWords = null;

            for( var i = 0; i < searched.Count; i++ ){
                if(searched[i].Contains("Не знайдено слова")){
                    botClient.SendTextMessageAsync(chat_id, $"Слова '{searched[i]}' не знайдено").Wait();
                    searched.RemoveAt(i);
                    continue;
                }
                if( ! searched[i].Contains((char)769) ){
                    botClient.SendTextMessageAsync(chat_id, $"Слово '{searched[i]}' має тільки один наоголос").Wait();
                    searched.RemoveAt(i);
                    continue;
                }
            }

            TextProcessing(ref searched);

            using(var cmd = new NpgsqlCommand($"insert into words(word) values ('{ string.Join("'), ('", searched) }');", DBConnection)){
                cmd.ExecuteNonQuery();
            }
            botClient.SendTextMessageAsync(chat_id, $"Додано слов{ (searched.Count == 1 ? "" : "а") }: {string.Join(", ", searched)}").Wait();


            return true;
        }
    }
}
