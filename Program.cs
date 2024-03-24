using NBitcoin;
using System.Diagnostics;
using System.Linq;
namespace test_btc
{
    internal class Program
    {


        // Biến để cache dữ liệu
        private static List<string> cachedData = new List<string>();
        private static HashSet<string> addData = new HashSet<string>();
        static async Task Main(string[] args)
        {
            string currentDirectory = Environment.CurrentDirectory;
            string filePath2 = Path.Combine(currentDirectory, "btc-list-address.txt");
            List<string> rd = new List<string>();
            int lengthFile = 2048;
            //   string filePath1 = Path.Combine(projectRootDirectory1, "china.txt");
            string filePath1 = Path.Combine(currentDirectory, "words_alpha.txt");
            await AddressExists(filePath2);
            List<string> data = await GetDataAsync(filePath1);
            int count = 0;
            Console.WriteLine($"Số dòng trong file: {data.Count}");

            string mnemonicWords = "";

            Random random = new Random();
            int seedNum = 12;
            while (true)
            {
                // seedNum = seedNum == 12 ? 24 : 12;
                rd = new List<string>();
                var listRd = new List<int>();
                mnemonicWords = string.Empty;
                for (int i = 0; i < seedNum; i++)
                {
                    bool b = true;
                    while (b)
                    {
                        int randomIndex = random.Next(lengthFile);
                        var check = listRd.Where(x => x == randomIndex);
                        if ((check == null || !check.Any()))
                        {
                            rd.Add(randomIndex.ToString());
                            listRd.Add(randomIndex);
                            mnemonicWords = mnemonicWords + " " + data[randomIndex];
                            b = false;
                        }
                    }

                }

                mnemonicWords = mnemonicWords.Trim();
                // if (!(!string.IsNullOrEmpty(mnemonicWords) && (mnemonicWords.Split(" ").Length == 12 || mnemonicWords.Split(" ").Length == 24))) continue;
                if (!(!string.IsNullOrEmpty(mnemonicWords) && (mnemonicWords.Split(" ").Length == 12))) continue;
                try
                {
                    count++;
                    Mnemonic mnemonic = new Mnemonic(mnemonicWords, Wordlist.English);
                    // Tạo master key từ mnemonic
                    ExtKey masterKey = mnemonic.DeriveExtKey();

                    // KeyPath cho mỗi loại địa chỉ
                    KeyPath keyPathSegwit = new KeyPath("m/84'/0'/0'/0/0"); // P2WPKH
                    KeyPath keyPathLegacy = new KeyPath("m/44'/0'/0'/0/0"); // P2PKH
                    KeyPath keyPathP2SH = new KeyPath("m/49'/0'/0'/0/0"); // P2SH-P2WPKH

                    var listAddress = new List<string>();
                    var extKey = masterKey.Derive(keyPathSegwit);
                    var address = extKey.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);

                    listAddress.Add(address.ToString());
                    var extKey1 = masterKey.Derive(keyPathLegacy);
                    var addres1s = extKey1.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);

                    listAddress.Add(addres1s.ToString());
                    var extKey2 = masterKey.Derive(keyPathP2SH);
                    var address2 = extKey2.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.SegwitP2SH, Network.Main);
                    listAddress.Add(address2.ToString());
                    Console.WriteLine($"[{count}]-{listAddress.Count}");
                    // Tạo và kiểm tra các loại địa chỉ khác nhau
                    // Stopwatch stopwatch = Stopwatch.StartNew();
                    await DeriveAndCheckBalance(listAddress,addData, mnemonicWords);
                    //stopwatch.Stop();
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }

            async Task DeriveAndCheckBalance(List<string> listAddress, HashSet<string> checkBtc, string mnemonicWords)
            {
                try
                {
                    // Tạo địa chỉ từ master key và key path
                    // Kiểm tra xem địa chỉ có trong file CSV không

                    bool addressFound = AddressExistsInCsv(checkBtc,listAddress);

                    if (addressFound)
                    {
                        string output = $"12 Seed: {mnemonicWords} | address:{String.Join(", ", listAddress)}";                    
                        string filePath = Path.Combine(currentDirectory, "btc-wallet.txt");

                        await using (StreamWriter sw = File.AppendText(filePath))
                        {
                            await sw.WriteLineAsync(output);
                        }
                        Console.WriteLine($"Thông tin đã được ghi vào file cho địa chỉ: {String.Join(", ", listAddress)}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }

            bool AddressExistsInCsv(HashSet<string> checkBtc, List<string> listAddress)
            {
                if (checkBtc.Count < 1)
                    return false;
                foreach (var VARIABLE in listAddress)
                {
                    if (checkBtc.Contains(VARIABLE))
                        return true;
                }
                return false;
            }
        }
        static async Task AddressExists(string csvFilePath)
        {
            string? line = "";
            if (addData.Count < 1)
            {
                Console.WriteLine("begin aync data !");
                using (var reader = new StreamReader(csvFilePath))
                {
                    // Đọc từng dòng trong tệp
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                            addData.Add(line);
                    }
                }
                Console.WriteLine("end aync data !");
            }
        }
        static async Task<List<string>> GetDataAsync(string filePath)
        {
            // Nếu dữ liệu đã được cache, trả về dữ liệu từ cache
            if (cachedData != null && cachedData.Count > 0)
            {
                Console.WriteLine("Lấy dữ liệu từ cache.");
                return cachedData;
            }

            // Nếu chưa có dữ liệu trong cache, đọc từ file
            Console.WriteLine("Đọc dữ liệu từ file và cache nó.");
            cachedData = new List<string>();

            // Kiểm tra xem file có tồn tại không
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File không tồn tại.");
                return cachedData;
            }

            // Đọc file và lưu vào cache
            using (StreamReader reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    cachedData.Add(line);
                }
            }

            return cachedData;
        }

    }
}
