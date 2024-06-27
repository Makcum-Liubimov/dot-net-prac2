using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Microsoft.Extensions.Hosting;



public partial class Program
{
    private static readonly string BotToken = "7372180707:AAHjCdIRxr57ehxt0ZME2Lk-PrqCAt3xs44";
    private static readonly TelegramBotClient BotClient = new TelegramBotClient(BotToken);

    private static readonly Dictionary<long, UserState> UserStates = new Dictionary<long, UserState>();

    public static async Task Main(string[] args)
    {

        var cts = new CancellationTokenSource();

        BotClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            },
            cancellationToken: cts.Token
        );

        var me = await BotClient.GetMeAsync();
        Console.WriteLine($"Start listening for @{me.Username}");

        await SendStartupMessageAsync();

        Console.ReadLine();
        cts.Cancel();
    }

    static async Task SendStartupMessageAsync()
    {
        long chatId = 622108674;  
        await BotClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Bot has started!"
        );
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message.Type != MessageType.Text)
            return;

        var message = update.Message;
        var chatId = message.Chat.Id;
        var text = message.Text;

        if (!UserStates.ContainsKey(chatId))
        {
            UserStates[chatId] = new UserState();
        }

        var userState = UserStates[chatId];

        if (text.StartsWith("/"))
        {
            switch (text.Split(' ')[0])
            {
                case "/start":
                    await HandleStartCommand(chatId);
                    break;
                case "/setcpu":
                    userState.CurrentStep = Step.AskCPU;
                    await botClient.SendTextMessageAsync(chatId, "Please enter your budget for the CPU:");
                    break;
                case "/setgpu":
                    userState.CurrentStep = Step.AskGPU;
                    await botClient.SendTextMessageAsync(chatId, "Please enter your budget for the GPU:");
                    break;
                case "/setram":
                    userState.CurrentStep = Step.AskRAM;
                    await botClient.SendTextMessageAsync(chatId, "Please enter your budget for the RAM:");
                    break;
                case "/setpsu":
                    userState.CurrentStep = Step.AskPSU;
                    await botClient.SendTextMessageAsync(chatId, "Please enter your budget for the PSU:");
                    break;
                case "/setmb":
                    userState.CurrentStep = Step.AskMB;
                    await botClient.SendTextMessageAsync(chatId, "Please enter your budget for the MB:");
                    break;
                case "/setcase":
                    userState.CurrentStep = Step.AskCASE;
                    await botClient.SendTextMessageAsync(chatId, "Please enter your budget for the CASE:");
                    break;
                case "/setcooling":
                    userState.CurrentStep = Step.AskCOOLING;
                    await botClient.SendTextMessageAsync(chatId, "Please enter your budget for the COOLING:");
                    break;
                case "/summary":
                    await HandleSummaryCommand(chatId, userState);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId, "Unknown command.");
                    break;
            }
        }
        else
        {
            switch (userState.CurrentStep)
            {
                case Step.AskCPU:
                    await HandleComponentSelection(chatId, text, "CPU", userState, GetCpuOptions);
                    break;
                case Step.AskGPU:
                    await HandleComponentSelection(chatId, text, "GPU", userState, GetGpuOptions);
                    break;
                case Step.AskRAM:
                    await HandleComponentSelection(chatId, text, "RAM", userState, GetRamOptions);
                    break;
                case Step.AskPSU:
                    await HandleComponentSelection(chatId, text, "PSU", userState, GetPsuOptions);
                    break;
                case Step.AskMB:
                    await HandleComponentSelection(chatId, text, "MB", userState, GetMbOptions);
                    break;
                case Step.AskCASE:
                    await HandleComponentSelection(chatId, text, "CASE", userState, GetCaseOptions);
                    break;
                case Step.AskCOOLING:
                    await HandleComponentSelection(chatId, text, "COOLING", userState, GetCoolingOptions);
                    break;
                case Step.SelectingCPU:
                    await HandleComponentChoice(chatId, text, "CPU", userState);
                    break;
                case Step.SelectingGPU:
                    await HandleComponentChoice(chatId, text, "GPU", userState);
                    break;
                case Step.SelectingRAM:
                    await HandleComponentChoice(chatId, text, "RAM", userState);
                    break;
                case Step.SelectingPSU:
                    await HandleComponentChoice(chatId, text, "PSU", userState);
                    break;
                case Step.SelectingMB:
                    await HandleComponentChoice(chatId, text, "MB", userState);
                    break;
                case Step.SelectingCASE:
                    await HandleComponentChoice(chatId, text, "CASE", userState);
                    break;
                case Step.SelectingCOOLING:
                    await HandleComponentChoice(chatId, text, "COOLING", userState);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId, "Please use a command to start.");
                    break;
            }
        }
    }

    static async Task HandleStartCommand(long chatId)
    {
        var welcomeMessage = "Welcome! Use the following commands to set your PC components:\n" +
                             "/setcpu - Set budget for CPU\n" +
                             "/setgpu - Set budget for GPU\n" +
                             "/setram - Set budget for RAM\n" +
                             "/setpsu - Set budget for PSU\n" +
                             "/setmb - Set budget for MB\n" +
                             "/setcase - Set budget for CASE\n" +
                             "/setcooling - Set budget for COOLING\n" +
                             "/summary - Show selected components and total price";

        await BotClient.SendTextMessageAsync(chatId, welcomeMessage);
    }

    static async Task HandleSummaryCommand(long chatId, UserState userState)
    {
        var summaryMessage = "Your selected components:\n" +
                             $"CPU: {userState.CPU}\n" +
                             $"GPU: {userState.GPU}\n" +
                             $"RAM: {userState.RAM}\n" +
                             $"PSU: {userState.PSU}\n" +
                             $"MB: {userState.MB}\n" +
                             $"CASE: {userState.CASE}\n" +
                             $"COOLING: {userState.COOLING}\n";

        await BotClient.SendTextMessageAsync(chatId, summaryMessage);
    }

    static async Task HandleComponentSelection(long chatId, string budgetText, string component, UserState userState, Func<decimal, List<ComponentOption>> getOptions)
    {
        if (decimal.TryParse(budgetText, out var budget))
        {
            var options = getOptions(budget);
            var optionsMessage = $"Here are the {component} options for your budget:\n";
            for (int i = 0; i < options.Count; i++)
            {
                optionsMessage += $"{i + 1} - {options[i].Name} (${options[i].Price})\n";
                if (!string.IsNullOrEmpty(options[i].ImageUrl))
                {
                    var inputFile = new InputOnlineFile(new Uri(options[i].ImageUrl));
                    await BotClient.SendPhotoAsync(chatId, inputFile, $"{options[i].Name} (${options[i].Price})");
                }
            }

            await BotClient.SendTextMessageAsync(chatId, optionsMessage);

            switch (component)
            {
                case "CPU":
                    userState.CurrentStep = Step.SelectingCPU;
                    userState.CpuOptions = options;
                    break;
                case "GPU":
                    userState.CurrentStep = Step.SelectingGPU;
                    userState.GpuOptions = options;
                    break;
                case "RAM":
                    userState.CurrentStep = Step.SelectingRAM;
                    userState.RamOptions = options;
                    break;
                case "PSU":
                    userState.CurrentStep = Step.SelectingPSU;
                    userState.PsuOptions = options;
                    break;
                case "MB":
                    userState.CurrentStep = Step.SelectingMB;
                    userState.MbOptions = options;
                    break;
                case "CASE":
                    userState.CurrentStep = Step.SelectingCASE;
                    userState.CaseOptions = options;
                    break;
                case "COOLING":
                    userState.CurrentStep = Step.SelectingCOOLING;
                    userState.CoolingOptions = options;
                    break;
            }
        }
        else
        {
            await BotClient.SendTextMessageAsync(chatId, "Invalid budget. Please enter a valid number.");
        }
    }




    static async Task HandleComponentChoice(long chatId, string choiceText, string component, UserState userState)
    {
        if (int.TryParse(choiceText, out var choice) && choice >= 1 && choice <= 3)
        {
            var option = component switch
            {
                "CPU" => userState.CpuOptions[choice - 1],
                "GPU" => userState.GpuOptions[choice - 1],
                "RAM" => userState.RamOptions[choice - 1],
                "PSU" => userState.PsuOptions[choice - 1],
                "MB" => userState.MbOptions[choice - 1],
                "CASE" => userState.CaseOptions[choice - 1],
                "COOLING" => userState.CoolingOptions[choice - 1],
                _ => null
            };

            switch (component)
            {
                case "CPU":
                    userState.CPU = $"{option.Name} (${option.Price})";
                    userState.CurrentStep = Step.None;
                    break;
                case "GPU":
                    userState.GPU = $"{option.Name} (${option.Price})";
                    userState.CurrentStep = Step.None;
                    break;
                case "RAM":
                    userState.RAM = $"{option.Name} (${option.Price})";
                    userState.CurrentStep = Step.None;
                    break;
                case "PSU":
                    userState.PSU = $"{option.Name} (${option.Price})";
                    userState.CurrentStep = Step.None;
                    break;
                case "MB":
                    userState.MB = $"{option.Name} (${option.Price})";
                    userState.CurrentStep = Step.None;
                    break;
                case "CASE":
                    userState.CASE = $"{option.Name} (${option.Price})";
                    userState.CurrentStep = Step.None;
                    break;
                case "COOLING":
                    userState.COOLING = $"{option.Name} (${option.Price})";
                    userState.CurrentStep = Step.None;
                    break;
            }

            await BotClient.SendTextMessageAsync(chatId, $"{component} selected: {option.Name} (${option.Price})");
        }
        else
        {
            await BotClient.SendTextMessageAsync(chatId, "Invalid choice. Please enter a number corresponding to an option.");
        }
    }

    static List<ComponentOption> GetCpuOptions(decimal budget)
    {
        return budget switch
        {
            <= 100 => new List<ComponentOption>
        {
            new ComponentOption { Name = "Intel i3", Price = 90, ImageUrl = "https://compx.ua/image/cache/catalog/products/64/25fe117e-5b7c-11ee-8f90-001b21ea407b-1400x1400.jpg" },
            new ComponentOption { Name = "AMD Ryzen 3", Price = 100, ImageUrl = "https://www.notebookcheck.net/fileadmin/Notebooks/Sonstiges/AMD/Ryzen/Ryzen_Desktop_3100_3300X/AMD_R3_3100_3300X_6.jpg" },
            new ComponentOption { Name = "Intel i5", Price = 110, ImageUrl = "https://click.ua/content/shop/products/64413/photos/protsessor-intel-core-i5-12400f-2-5ghz-18mb-alder-lake-65w-s1700-box-bx8071512400f-338x310-8541.jpg" }
        },
            <= 300 => new List<ComponentOption>
        {
            new ComponentOption { Name = "Intel i5", Price = 290, ImageUrl = "https://click.ua/content/shop/products/64413/photos/protsessor-intel-core-i5-12400f-2-5ghz-18mb-alder-lake-65w-s1700-box-bx8071512400f-338x310-8541.jpg" },
            new ComponentOption { Name = "AMD Ryzen 5", Price = 300, ImageUrl = "https://artline.ua/storage/images/products/7011/gallery/89613/600_products_1668068977532242_0.webp" },
            new ComponentOption { Name = "Intel i7", Price = 310, ImageUrl = "https://artline.ua/storage/images/products/14231/gallery/172807/600_gallery_1690359738113492_0.webp" }
        },
            _ => new List<ComponentOption>
        {
            new ComponentOption { Name = "Intel i7", Price = 490, ImageUrl = "https://artline.ua/storage/images/products/14231/gallery/172807/600_gallery_1690359738113492_0.webp" },
            new ComponentOption { Name = "AMD Ryzen 7", Price = 500, ImageUrl = "https://f.428.ua/img/3413621/600/600/protsessor_amd_ryzen_7_5700x_s-am4_3_4ghz_32mb_tray_100-000000926~584~583.jpg" },
            new ComponentOption { Name = "Intel i9", Price = 510, ImageUrl = "https://images.prom.ua/3634888837_w640_h640_protsessor-intel-core.jpg" }
        }
        };
    }


    static List<ComponentOption> GetGpuOptions(decimal budget)
    {
        return budget switch
        {
            <= 100 => new List<ComponentOption>
            {
                new ComponentOption { Name = "NVIDIA GTX 1050", Price = 90, ImageUrl = "https://compbest.com.ua/content/images/37/diskretnaya_videokarta_nvidia_geforce_gt_1050_2gb_gddr5-47960981726646_small11.jpg" },
                new ComponentOption { Name = "AMD RX 560", Price = 100, ImageUrl = "https://img.telemart.ua/491694-630769-product_popup/asus-rog-radeon-rx-560-strix-4096mb-rog-strix-rx560-4g-v2-gaming.jpg" },
                new ComponentOption { Name = "NVIDIA GTX 1650", Price = 110, ImageUrl = "https://m.media-amazon.com/images/I/81u9V1mL8GL.jpg" }
            },
            <= 300 => new List<ComponentOption>
            {
                new ComponentOption { Name = "NVIDIA GTX 1660", Price = 290, ImageUrl = "https://img.telemart.ua/201022-390647-product_popup/asus-geforce-gtx-1660-super-phoenix-oc-6144mb-ph-gtx1660s-o6g.jpg" },
                new ComponentOption { Name = "AMD RX 580", Price = 300, ImageUrl = "https://content2.rozetka.com.ua/goods/images/big/324160844.jpg" },
                new ComponentOption { Name = "NVIDIA GTX 2060", Price = 310, ImageUrl = "https://m.media-amazon.com/images/I/71psWySiMAL._AC_SL1500_.jpg" }
            },
            _ => new List<ComponentOption>
            {
                new ComponentOption { Name = "NVIDIA RTX 3060", Price = 690, ImageUrl = "https://hotline.ua/img/tx/438/4387045285.jpg" },
                new ComponentOption { Name = "AMD RX 6700", Price = 700, ImageUrl = "https://xcom.ua/images/items/big/562359.jpg" },
                new ComponentOption { Name = "NVIDIA RTX 3070", Price = 710, ImageUrl = "https://static.mti.ua/product/25488/gQonY3FzrGDkKWRs2M8LjfEZTSgiK0aOXe3eWk9P.png" }
            }
        };
    }

    static List<ComponentOption> GetRamOptions(decimal budget)
    {
        return budget switch
        {
            <= 50 => new List<ComponentOption>
            {
                new ComponentOption { Name = "8GB Corsair Vengeance", Price = 40, ImageUrl = "https://hotline.ua/img/tx/179/1799125485.jpg" },
                new ComponentOption { Name = "8GB G.Skill Ripjaws", Price = 50, ImageUrl = "https://www.ryans.com/storage/products/main/gskill-ripjaws-8gb-ddr4-2666mhz-desktop-ram-11552193613.webp" },
                new ComponentOption { Name = "8GB Kingston HyperX", Price = 60, ImageUrl = "https://hotline.ua/img/tx/148/1489555585.jpg" }
            },
            <= 100 => new List<ComponentOption>
            {
                new ComponentOption { Name = "16GB Corsair Vengeance", Price = 90, ImageUrl = "https://m.media-amazon.com/images/I/816t6aP2NoL._AC_UF1000,1000_QL80_.jpg" },
                new ComponentOption { Name = "16GB G.Skill Ripjaws", Price = 100, ImageUrl = "https://content1.rozetka.com.ua/goods/images/original/400963923.jpg" },
                new ComponentOption { Name = "16GB Kingston HyperX", Price = 110, ImageUrl = "https://hotline.ua/img/tx/148/1489555705.jpg" }
            },
            _ => new List<ComponentOption>
            {
                new ComponentOption { Name = "32GB Corsair Vengeance", Price = 110, ImageUrl = "https://hotline.ua/img/tx/343/3433563715.jpg" },
                new ComponentOption { Name = "32GB G.Skill Ripjaws", Price = 120, ImageUrl = "https://hotline.ua/img/tx/457/4574982635.jpg" },
                new ComponentOption { Name = "32GB Kingston HyperX", Price = 130, ImageUrl = "https://hotline.ua/img/tx/197/1972040615.jpg" }
            }
        };
    }

    static List<ComponentOption> GetPsuOptions(decimal budget)
    {
        return budget switch
        {
            <= 80 => new List<ComponentOption>
            {
                new ComponentOption { Name = "500W EVGA", Price = 70, ImageUrl = "https://content.rozetka.com.ua/goods/images/big/87587444.jpg" },
                new ComponentOption { Name = "500W Corsair", Price = 80, ImageUrl = "https://images.tcdn.com.br/img/img_prod/1042614/fonte_corsair_500w_80_plus_white_vs500_cp_9020223_br_1731_3_6ae9cc8a2f940ce1613c69aeed1a81e8_20220606081223.jpg" },
                new ComponentOption { Name = "500W Thermaltake", Price = 90, ImageUrl = "https://artline.ua/storage/images/products/11843/gallery/146667/600_gallery_1679393069388075_0.webp" }
            },
            <= 150 => new List<ComponentOption>
            {
                new ComponentOption { Name = "750W EVGA", Price = 140, ImageUrl = "https://m.media-amazon.com/images/I/71P7d31fERL.jpg" },
                new ComponentOption { Name = "750W Corsair", Price = 150, ImageUrl = "https://brain.com.ua/static/images/prod_img/0/7/U0811007_2big.jpg" },
                new ComponentOption { Name = "750W Thermaltake", Price = 160, ImageUrl = "https://hotline.ua/img/tx/444/4442349585.jpg" }
            },
            _ => new List<ComponentOption>
            {
                new ComponentOption { Name = "1000W EVGA", Price = 190, ImageUrl = "https://mobileplanet.ua/uploads/product/2023-12-22/evga-supernova-gq-1000w-80-plus-gold-210-gq-1000-v-298772.webp" },
                new ComponentOption { Name = "1000W Corsair", Price = 200, ImageUrl = "https://m.media-amazon.com/images/I/71Y37Cq0xZL._AC_UF350,350_QL80_.jpg" },
                new ComponentOption { Name = "1000W Thermaltake", Price = 210, ImageUrl = "https://robby.com.ua/image/cache/catalog/tovar/files/thermaltake-toughpower-gf3-1000w-80-plus-gold-atx-30/pr_2022_11_30_15_56_17_3_00-850x850.jpg" }
            }
        };
    }

    static List<ComponentOption> GetMbOptions(decimal budget)
    {
        return budget switch
        {
            <= 80 => new List<ComponentOption>
            {
                new ComponentOption { Name = "ASUS Prime", Price = 70, ImageUrl = "https://click.ua/content/shop/products/65589/photos/materinskaya-plata-asus-prime-b660m-k-d4-socket-1700-800x498-fa3c.jpg" },
                new ComponentOption { Name = "Gigabyte UD", Price = 80, ImageUrl = "https://systema.kg/411328-large_default/mb-lga1700-gigabyte-b760-ds3h-ddr44xddr412xusb6xsataiiiatxpcie16x-2pcie1xvga-hdmi2xdp.jpg" },
                new ComponentOption { Name = "MSI ", Price = 90, ImageUrl = "https://www.techpowerup.com/img/17-03-02/67034b35751e.jpg" }
            },
            <= 150 => new List<ComponentOption>
            {
                new ComponentOption { Name = "ASUS TUF", Price = 140, ImageUrl = "https://img.telemart.ua/437881-586152-product_popup/asus-tuf-gaming-b650-plus-wifi.jpg" },
                new ComponentOption { Name = "Gigabyte Aorus", Price = 150, ImageUrl = "https://luxelectro.com.ua/images/stories/virtuemart/product/gigabyte/B550I-AORUS-PRO-AX.jpg" },
                new ComponentOption { Name = "MSI MPG", Price = 160, ImageUrl = "https://wakcomputer.com/wp-content/uploads/2024/01/LD0005689669_1d-1.jpg" }
            },
            _ => new List<ComponentOption>
            {
                new ComponentOption { Name = "ASUS ROG", Price = 190, ImageUrl = "https://i.moyo.ua/img/products/5261/69_4000.jpg" },
                new ComponentOption { Name = "Gigabyte Aorus Master", Price = 200, ImageUrl = "https://luxelectro.com.ua/images/stories/virtuemart/product/gigabyte/Z790-AORUS-MASTER.jpg" },
                new ComponentOption { Name = "MSI MEG", Price = 210, ImageUrl = "https://cdn11.bigcommerce.com/s-sp9oc95xrw/images/stencil/1280x1280/products/3361/22209/13-144-382-07__48656.1617879394.jpg?c=2" }
            }
        };
    }

    static List<ComponentOption> GetCaseOptions(decimal budget)
    {
        return budget switch
        {
            <= 50 => new List<ComponentOption>
            {
                new ComponentOption { Name = "NZXT H510", Price = 40, ImageUrl = "https://hotline.ua/img/tx/341/3419837925.jpg" },
                new ComponentOption { Name = "Corsair 275R", Price = 50, ImageUrl = "https://hotline.ua/img/tx/169/1695860935.jpg" },
                new ComponentOption { Name = "Cooler Master NR400", Price = 60, ImageUrl = "https://img.telemart.ua/304494-471633-product_popup/cooler-master-masterbox-nr400-without-odd-tempered-glass-bez-bp-mcb-nr400-kgnn-s00-black.png" }
            },
            <= 120 => new List<ComponentOption>
            {
                new ComponentOption { Name = "NZXT H510 Elite", Price = 110, ImageUrl = "https://img.telemart.ua/183899-378301/nzxt-h510-elite-ca-h510e-w1-matte-whiteblack.jpg" },
                new ComponentOption { Name = "Corsair 4000X", Price = 120, ImageUrl = "https://brain.com.ua/static/images/prod_img/8/7/U0489387_big.jpg" },
                new ComponentOption { Name = "Cooler Master H500", Price = 130, ImageUrl ="https://hotline.ua/img/tx/273/2738647135.jpg" }
            },
            _ => new List<ComponentOption>
            {
                new ComponentOption { Name = "NZXT H710", Price = 140, ImageUrl = "https://goodsmart.in.ua/image/cache/catalog/product/4/800563731-korpus-nzxt-h710-matte-white-black-ca-h710b-w1-bez-bp-700x700.jpg" },
                new ComponentOption { Name = "Corsair 5000X", Price = 150, ImageUrl = "https://e.428.ua/img/3485236/3000/2000/korpus_corsair_icue_5000x_rgb_ql_tempered_glass_b_bp_white_cc-9011233-ww~949~1200.jpg" },
                new ComponentOption { Name = "Cooler Master H500P", Price = 160, ImageUrl = "https://hotline.ua/img/tx/273/2738643185.jpg" }
            }
        };
    }

    static List<ComponentOption> GetCoolingOptions(decimal budget)
    {
        return budget switch
        {
            <= 20 => new List<ComponentOption>
            {
                new ComponentOption { Name = "Cooler Master Hyper 212", Price = 15, ImageUrl = "https://hotline.ua/img/tx/454/4540960455.jpg" },
                new ComponentOption { Name = "Noctua NH-U12S", Price = 20, ImageUrl = "https://hotline.ua/img/tx/111/11175345.jpg" },
                new ComponentOption { Name = "Be Quiet! Pure Rock", Price = 25, ImageUrl = "https://images.prom.ua/4488566028_w600_h600_4488566028.jpg" }
            },
            <= 50 => new List<ComponentOption>
            {
                new ComponentOption { Name = "Cooler Master ML240L", Price = 40, ImageUrl = "https://files.coolermaster.com/og-image/masterliquid-ml240l-v2-rgb-1200x630.jpg" },
                new ComponentOption { Name = "Corsair H100i", Price = 50, ImageUrl = "https://content2.rozetka.com.ua/goods/images/original/331989914.jpg" },
                new ComponentOption { Name = "NZXT Kraken X53", Price = 60, ImageUrl = "https://hotline.ua/img/tx/461/4617626115.jpg" }
            },
            _ => new List<ComponentOption>
            {
                new ComponentOption { Name = "Cooler Master ML360R", Price = 90, ImageUrl = "https://hotline.ua/img/tx/444/4442126755.jpg" },
                new ComponentOption { Name = "Corsair H150i", Price = 100, ImageUrl = "https://content1.rozetka.com.ua/goods/images/big/331993624.jpg"  },
                new ComponentOption { Name = "NZXT Kraken X73", Price = 110, ImageUrl = "https://hotline.ua/img/tx/273/2731094565.jpg" }
            }
        };
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}

class UserState
{
    public string CPU { get; set; } = string.Empty;
    public string GPU { get; set; } = string.Empty;
    public string RAM { get; set; } = string.Empty;
    public string PSU { get; set; } = string.Empty;
    public string MB { get; set; } = string.Empty;
    public string CASE { get; set; } = string.Empty;
    public string COOLING { get; set; } = string.Empty;
    public Step CurrentStep { get; set; } = Step.None;
    public List<ComponentOption> CpuOptions { get; set; }
    public List<ComponentOption> GpuOptions { get; set; }
    public List<ComponentOption> RamOptions { get; set; }
    public List<ComponentOption> PsuOptions { get; set; }
    public List<ComponentOption> MbOptions { get; set; }
    public List<ComponentOption> CaseOptions { get; set; }
    public List<ComponentOption> CoolingOptions { get; set; }
}

class ComponentOption
{
    public string Name { get; set; }
    public decimal Price { get; set; }

    public string ImageUrl { get; set; }
}

enum Step
{
    None,
    AskCPU,
    AskGPU,
    AskRAM,
    AskPSU,
    AskMB,
    AskCASE,
    AskCOOLING,
    SelectingCPU,
    SelectingGPU,
    SelectingRAM,
    SelectingPSU,
    SelectingMB,
    SelectingCASE,
    SelectingCOOLING
}
