﻿using Npgsql;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot;
using System.Threading;
using Telegram.Bot.Args;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace telegram_stress_checker
{

    class ApplicationData{

        public string telegram_token { get; set; }
        public string db_connection_string { get; set; }

        public string stresses_file { get; set; } = "stresses.txt";
        public string invite_code { get; set; }

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

    class Program
    {
        static ApplicationData applicationData;
        static TelegramBotClient botClient;
        static NpgsqlConnection DBConnection;
        static readonly char[] vowels = new char[] {'а', 'е', 'и', 'і', 'о', 'у', 'я', 'ю', 'є', 'ї'};
        static void Main(string[] args)
        {
            using ( var stream = File.OpenRead("config.json") ){
                applicationData = JsonSerializer.DeserializeAsync<ApplicationData>(stream).Result;
            }

            DBConnection  = new NpgsqlConnection(applicationData.db_connection_string);

            // stresseRestore();

            botClient = new Telegram.Bot.TelegramBotClient(applicationData.telegram_token);

            botClient.OnMessage += onMessageSend;
            botClient.OnMessageEdited += onMessageChange;
            botClient.StartReceiving();

            Task.Delay(Timeout.Infinite).Wait();
        }
        static async void onMessageSend(object sender, MessageEventArgs e){
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
                            await Game(e.Message.From.Id, e.Message.Chat.Id, message.Substring(1).Contains(" ") ? Convert.ToUInt16(message.Substring(1).Split(" ")[1]) : (uint)20);
                            break;
                        default:
                            await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Незрозуміла команда");
                            break;
                    }
                }else{
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Це так не працює, напиши /help для додаткової інформації", replyToMessageId: e.Message.MessageId);
                }

            }else if(e.Message.Text != null){
                if(e.Message.Text == "Готово"){
                    await Game(e.Message.From.Id, e.Message.Chat.Id, nextWord: true);
                }else if(!e.Message.Text.Contains("/") && !e.Message.Text.Contains(" ")){
                    using (var cmd = new NpgsqlCommand($"update users set answers = array_append(answers, '{e.Message.Text}'), ingame = true where userid = @p", DBConnection)){
                        cmd.Parameters.AddWithValue("p", e.Message.From.Id);
                        cmd.ExecuteNonQueryAsync().Wait();
                    }
                }
            }else{

                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Я сприймаю тільки текст, можливо в майбутньому це зміниться, напиши /help для додаткової інформації");

            }
        }
        static async void onMessageChange(object sender, MessageEventArgs e){
            if(e.Message.Text != null){
                int violation = 1;
                await using (var cmd = new NpgsqlCommand($"SELECT violation FROM users WHERE userid="+e.Message.From.Id.ToString(), DBConnection))
                await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync()){
                    Console.WriteLine(reader.GetInt32(0));
                    violation += reader.GetInt32(0);
                }
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"Changing text isn't allowed. If you change any text you entered, you will have {3-violation} warnings and then you will get ban\n2 attempts left");
                using (var cmd = new NpgsqlCommand("update words set violation = @v where userid = {e.Message.From.Id}", DBConnection)){
                    cmd.Parameters.AddWithValue("v", violation);
                    cmd.ExecuteNonQueryAsync().Wait();
                }
            }
        }
        static async Task StartMessage(long chat_id){
            await botClient.SendTextMessageAsync(chat_id, @"Це бот для перевірки наголосів. Для того, щоб це зробити, вам потрібно:
1) Зареєструватися за допомогою
/register <код запрошення>
    для перевірки, чи ви вже зереєстровані
/is_registered <- 
2) Власне почати гру
/start_game <кількість слів ( за промовчкванням 20 )>

Якщо ви пам'ятаєте, як потрібно наголошувати слово:
/search слово

/help <- для інформації про бота
");
        }
        static async Task<bool> in_proggress(int user_id){
            try{
                await using (var cmd = new NpgsqlCommand($"SELECT ingame FROM users WHERE userid = {user_id};", DBConnection))
                await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync()){
                    return reader.GetFieldValue<bool>(0);
                }
            }catch(Npgsql.NpgsqlOperationInProgressException){
                return false;
            }
            return false;
        }
        static Exception registerUser(int user_id, string key){
            if(applicationData.invite_code == null || key != applicationData.invite_code){
                return applicationData.invite_code == null ? new Exception("Пусте поле коду запрошення") : new Exception("Не правильне поле коду запрошення");
            }
            if(isRegistered(user_id).Result){
                return new Exception("Ви вже зареєстровані");
            }
            try{
                using (var cmd = new NpgsqlCommand("insert into users (userid) values (@v);", DBConnection)){
                    cmd.Parameters.AddWithValue("v", user_id);
                    cmd.ExecuteNonQueryAsync().Wait();
                }
            }catch(System.AggregateException e){
                Console.WriteLine(e.Data);
                return new Exception("You have alredy registered");
            }

            return null;
        }
        static async Task<bool> isRegistered(int user_id, long? chat_id = null){
            
            await using (var cmd = new NpgsqlCommand($"SELECT id FROM users WHERE userid="+user_id, DBConnection))
            await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()){
                Console.WriteLine(reader.GetInt32(0));
                if(chat_id==null){
                    return reader.FieldCount != 0;
                }
                if(reader.FieldCount != 0){
                    await botClient.SendTextMessageAsync(chat_id, "Ви зареєстровані");
                }else{
                    await botClient.SendTextMessageAsync(chat_id, "Ви не зареєстровані");
                }
            }

            return false;
        }
        static bool appDataUpdate(){
            using ( var stream = File.OpenRead("config.json") ){
                applicationData = JsonSerializer.DeserializeAsync<ApplicationData>(stream).Result;
            }

            DBConnection  = new NpgsqlConnection(applicationData.db_connection_string);

            return true;
        }
        static bool appDataBackup(){
            string backup = JsonSerializer.Serialize<ApplicationData>(applicationData);
            using (var stream = new StreamWriter("config.json")){
                stream.Write(backup);
            } 
            return true;
        }
        static bool stresseRestore(){

            using (var stream = new StreamReader(applicationData.stresses_file)){
                DBConnection.Open();
                while(!stream.EndOfStream){
                    string line  = stream.ReadLine();
                    Words words = new Words(line, line.Contains(" "));
                    using (var cmd = new NpgsqlCommand("INSERT INTO words (word, definition) VALUES (@w, @p)", DBConnection)){
                        cmd.Parameters.AddWithValue("w", words.word);
                        cmd.Parameters.AddWithValue("p", words.definition ?? "");
                        cmd.ExecuteNonQueryAsync().Wait();
                    }
                }
            }
            return true;
        }
        static async Task Game(long user_id, long chat_id, uint amount = 20, bool nextWord = false){
            List<int> word_array = new List<int>();
            if(!(await in_proggress((int)user_id))){
                // SELECT * FROM words WHERE random() < 0.01 LIMIT 20;
                await using (var cmd = new NpgsqlCommand($"SELECT id FROM words WHERE random() < 0.01 LIMIT {amount};", DBConnection))
                await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync()){
                    word_array.Add(reader.GetInt32(0));
                }
                using (var cmd = new NpgsqlCommand($"update users set words = @w, ingame = true where userid = @p", DBConnection)){
                    cmd.Parameters.AddWithValue("w", word_array);
                    cmd.Parameters.AddWithValue("p", user_id);
                    cmd.ExecuteNonQueryAsync().Wait();
                }
            }else{
                await using (var cmd = new NpgsqlCommand($"SELECT words FROM users WHERE userid = {user_id};", DBConnection))
                await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync()){
                    word_array = reader.GetFieldValue<List<int>>(0);
                }
                if(word_array.Count == 0 || word_array == null){
                    await using(var comand = new NpgsqlCommand($"update users set ingame=false where userid={user_id};", DBConnection)){
                        await comand.ExecuteNonQueryAsync();
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
                    }catch(System.InvalidCastException){
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
                        await botClient.SendTextMessageAsync(chat_id, $"Чудово, усе правильно", 
                            replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove());
                    }else{
                        await botClient.SendTextMessageAsync(chat_id, $"У вас правильних відповідей: {correctAns}", 
                            replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove());
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
        static IEnumerable<string> AllOptions(string word){
            
            List<string> res = new List<string>();

            for(var i = 0;i < word.Length; i++){
                if(vowels.Contains(char.ToLower(word[i]))){
                    var w = new System.Text.StringBuilder(word.ToLower());
                    w[i] = char.ToUpper(w[i]);
                    res.Add(w.ToString());
                }
            }

            return res;
        }
        static IEnumerable<string> CorrectOptions(string correctOption){

            List<string> res = new List<string>();

            for(var i = 0;i < correctOption.Length; i++){
                if(char.ToUpper(correctOption[i]) == correctOption[i]){
                    var w = new System.Text.StringBuilder(correctOption.ToLower());
                    w[i] = char.ToUpper(w[i]);
                    res.Add(w.ToString());
                }
            }

            return res;
        }
        static void KeyboardAppend(out IEnumerable<IEnumerable<Telegram.Bot.Types.ReplyMarkups.KeyboardButton>> keyboard, string[] options, int colSize){
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
    }
}
