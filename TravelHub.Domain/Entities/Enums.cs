using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TravelHub.Domain.Entities;

public enum Status
{
    Planning,
    Finished
}

public enum TransportationType
{
    Car,
    Motorcycle,
    Plane,
    Ship,
    Ferry,
    Taxi,
    Bus,
    Walk
}

public enum Rating
{
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5
}

public enum TripParticipantStatus
{
    Pending,
    Accepted,
    Declined,
    Owner
}

public enum FriendRequestStatus
{
    Pending,
    Accepted,
    Declined,
    Blocked
}

public enum BlogVisibility
{
    [Display(Name = "Only trip participants")]
    Private = 0,

    [Display(Name = "Only trip owner's friends")]
    ForMyFriends = 1,

    [Display(Name = "Friends of all trip participants")]
    ForTripParticipantsFriends = 2,

    [Display(Name = "Public")]
    Public = 3
}

public enum CurrencyCode
{
    // A
    [Display(Name = "Afghan Afghani")]
    AFN,

    [Display(Name = "Albanian Lek")]
    ALL,

    [Display(Name = "Algerian Dinar")]
    DZD,

    [Display(Name = "Angolan Kwanza")]
    AOA,

    [Display(Name = "Eastern Caribbean Dollar")]
    XCD, // Antigua i Barbuda, Dominika, Grenada, Saint Kitts i Nevis, Saint Lucia, Saint Vincent i Grenadyny

    [Display(Name = "Argentine Peso")]
    ARS,

    [Display(Name = "Armenian Dram")]
    AMD,

    [Display(Name = "Aruban Florin")]
    AWG,

    // B
    [Display(Name = "Australian Dollar")]
    AUD, // Australia, Kiribati, Nauru, Tuvalu

    [Display(Name = "Azerbaijani Manat")]
    AZN,

    [Display(Name = "Bahamian Dollar")]
    BSD,

    [Display(Name = "Bahraini Dinar")]
    BHD,

    [Display(Name = "Bangladeshi Taka")]
    BDT,

    [Display(Name = "Barbadian Dollar")]
    BBD,

    [Display(Name = "Belarusian Ruble")]
    BYN,

    [Display(Name = "Belize Dollar")]
    BZD,

    [Display(Name = "West African CFA Franc")]
    XOF, // Benin, Burkina Faso, Gwinea Bissau, Wybrzeże Kości Słoniowej, Mali, Niger, Senegal, Togo

    [Display(Name = "Bermudian Dollar")]
    BMD,

    [Display(Name = "Bhutanese Ngultrum")]
    BTN,

    [Display(Name = "Bolivian Boliviano")]
    BOB,

    [Display(Name = "Bosnia-Herzegovina Convertible Mark")]
    BAM,

    [Display(Name = "Botswana Pula")]
    BWP,

    [Display(Name = "Brazilian Real")]
    BRL,

    [Display(Name = "Brunei Dollar")]
    BND,

    [Display(Name = "Bulgarian Lev")]
    BGN,

    [Display(Name = "Burundian Franc")]
    BIF,

    // C
    [Display(Name = "Cape Verdean Escudo")]
    CVE,

    [Display(Name = "Cambodian Riel")]
    KHR,

    [Display(Name = "Central African CFA Franc")]
    XAF, // Kamerun, Czad, Republika Konga, Gabon, Gwinea Równikowa, Republika Środkowoafrykańska

    [Display(Name = "Canadian Dollar")]
    CAD,

    [Display(Name = "Cayman Islands Dollar")]
    KYD,

    [Display(Name = "Chilean Peso")]
    CLP,

    [Display(Name = "Chinese Yuan Renminbi")]
    CNY,

    [Display(Name = "Colombian Peso")]
    COP,

    [Display(Name = "Comorian Franc")]
    KMF,

    [Display(Name = "Congolese Franc")]
    CDF,

    [Display(Name = "Costa Rican Colón")]
    CRC,

    [Display(Name = "Croatian Kuna")]
    HRK,

    [Display(Name = "Cuban Peso")]
    CUP,

    [Display(Name = "Czech Koruna")]
    CZK,

    // D
    [Display(Name = "Danish Krone")]
    DKK, // Dania, Grenlandia, Wyspy Owcze

    [Display(Name = "Djiboutian Franc")]
    DJF,

    [Display(Name = "Dominican Peso")]
    DOP,

    // E
    [Display(Name = "Egyptian Pound")]
    EGP,

    [Display(Name = "Eritrean Nakfa")]
    ERN,

    [Display(Name = "Ethiopian Birr")]
    ETB,

    [Display(Name = "Euro")]
    EUR, // Andora, Austria, Belgia, Chorwacja, Cypr, Estonia, Finlandia, Francja, Niemcy, Grecja, Irlandia, Włochy, Kosowo, Łotwa, Litwa, Luksemburg, Malta, Monako, Czarnogóra, Holandia, Portugalia, San Marino, Słowacja, Słowenia, Hiszpania, Watykan

    // F
    [Display(Name = "Fijian Dollar")]
    FJD,

    // G
    [Display(Name = "Gambian Dalasi")]
    GMD,

    [Display(Name = "Georgian Lari")]
    GEL,

    [Display(Name = "Ghanaian Cedi")]
    GHS,

    [Display(Name = "Gibraltar Pound")]
    GIP,

    [Display(Name = "Guatemalan Quetzal")]
    GTQ,

    [Display(Name = "Guinean Franc")]
    GNF,

    [Display(Name = "Guyanese Dollar")]
    GYD,

    // H
    [Display(Name = "Haitian Gourde")]
    HTG,

    [Display(Name = "Honduran Lempira")]
    HNL,

    [Display(Name = "Hong Kong Dollar")]
    HKD,

    [Display(Name = "Hungarian Forint")]
    HUF,

    // I
    [Display(Name = "Icelandic Króna")]
    ISK,

    [Display(Name = "Indian Rupee")]
    INR,

    [Display(Name = "Indonesian Rupiah")]
    IDR,

    [Display(Name = "Iranian Rial")]
    IRR,

    [Display(Name = "Iraqi Dinar")]
    IQD,

    [Display(Name = "Israeli New Shekel")]
    ILS,

    // J
    [Display(Name = "Jamaican Dollar")]
    JMD,

    [Display(Name = "Japanese Yen")]
    JPY,

    [Display(Name = "Jordanian Dinar")]
    JOD,

    // K
    [Display(Name = "Kazakhstani Tenge")]
    KZT,

    [Display(Name = "Kenyan Shilling")]
    KES,

    [Display(Name = "Kuwaiti Dinar")]
    KWD,

    [Display(Name = "Kyrgyzstani Som")]
    KGS,

    // L
    [Display(Name = "Lao Kip")]
    LAK,

    [Display(Name = "Lebanese Pound")]
    LBP,

    [Display(Name = "Lesotho Loti")]
    LSL,

    [Display(Name = "Liberian Dollar")]
    LRD,

    [Display(Name = "Libyan Dinar")]
    LYD,

    // M
    [Display(Name = "Macanese Pataca")]
    MOP,

    [Display(Name = "Malagasy Ariary")]
    MGA,

    [Display(Name = "Malawian Kwacha")]
    MWK,

    [Display(Name = "Malaysian Ringgit")]
    MYR,

    [Display(Name = "Maldivian Rufiyaa")]
    MVR,

    [Display(Name = "Mauritanian Ouguiya")]
    MRU,

    [Display(Name = "Mauritian Rupee")]
    MUR,

    [Display(Name = "Mexican Peso")]
    MXN,

    [Display(Name = "Moldovan Leu")]
    MDL,

    [Display(Name = "Mongolian Tögrög")]
    MNT,

    [Display(Name = "Moroccan Dirham")]
    MAD, // Maroko, Sahara Zachodnia

    [Display(Name = "Mozambican Metical")]
    MZN,

    [Display(Name = "Myanmar Kyat")]
    MMK,

    // N
    [Display(Name = "Namibian Dollar")]
    NAD,

    [Display(Name = "Nepalese Rupee")]
    NPR,

    [Display(Name = "New Taiwan Dollar")]
    TWD,

    [Display(Name = "New Zealand Dollar")]
    NZD,

    [Display(Name = "Nicaraguan Córdoba")]
    NIO,

    [Display(Name = "Nigerian Naira")]
    NGN,

    [Display(Name = "North Korean Won")]
    KPW,

    [Display(Name = "Norwegian Krone")]
    NOK,

    // O
    [Display(Name = "Omani Rial")]
    OMR,

    // P
    [Display(Name = "Pakistani Rupee")]
    PKR,

    [Display(Name = "Panamanian Balboa")]
    PAB,

    [Display(Name = "Papua New Guinean Kina")]
    PGK,

    [Display(Name = "Paraguayan Guaraní")]
    PYG,

    [Display(Name = "Peruvian Sol")]
    PEN,

    [Display(Name = "Philippine Peso")]
    PHP,

    [Display(Name = "Polish Złoty")]
    PLN,

    // Q
    [Display(Name = "Qatari Riyal")]
    QAR,

    // R
    [Display(Name = "Romanian Leu")]
    RON,

    [Display(Name = "Russian Ruble")]
    RUB,

    [Display(Name = "Rwandan Franc")]
    RWF,

    // S
    [Display(Name = "Saint Helena Pound")]
    SHP,

    [Display(Name = "Samoan Tālā")]
    WST,

    [Display(Name = "São Tomé and Príncipe Dobra")]
    STN,

    [Display(Name = "Saudi Riyal")]
    SAR,

    [Display(Name = "Serbian Dinar")]
    RSD,

    [Display(Name = "Seychellois Rupee")]
    SCR,

    [Display(Name = "Sierra Leonean Leone")]
    SLL,

    [Display(Name = "Singapore Dollar")]
    SGD,

    [Display(Name = "Solomon Islands Dollar")]
    SBD,

    [Display(Name = "Somali Shilling")]
    SOS,

    [Display(Name = "South African Rand")]
    ZAR,

    [Display(Name = "South Korean Won")]
    KRW,

    [Display(Name = "South Sudanese Pound")]
    SSP,

    [Display(Name = "Sri Lankan Rupee")]
    LKR,

    [Display(Name = "Sudanese Pound")]
    SDG,

    [Display(Name = "Surinamese Dollar")]
    SRD,

    [Display(Name = "Swedish Krona")]
    SEK,

    [Display(Name = "Swiss Franc")]
    CHF,

    [Display(Name = "Syrian Pound")]
    SYP,

    // T
    [Display(Name = "Tajikistani Somoni")]
    TJS,

    [Display(Name = "Tanzanian Shilling")]
    TZS,

    [Display(Name = "Thai Baht")]
    THB,

    [Display(Name = "Tongan Paʻanga")]
    TOP,

    [Display(Name = "Trinidad and Tobago Dollar")]
    TTD,

    [Display(Name = "Tunisian Dinar")]
    TND,

    [Display(Name = "Turkish Lira")]
    TRY,

    [Display(Name = "Turkmenistani Manat")]
    TMT,

    // U
    [Display(Name = "Ugandan Shilling")]
    UGX,

    [Display(Name = "Ukrainian Hryvnia")]
    UAH,

    [Display(Name = "Emirati Dirham")]
    AED,

    [Display(Name = "British Pound Sterling")]
    GBP, // Wielka Brytania, Wyspa Man, Jersey, Guernsey, Gibraltar, Falklandy, Święta Helena

    [Display(Name = "US Dollar")]
    USD, // USA, Salwador, Panama (równolegle z PAB), Zimbabwe (równolegle), Mikronezja, Palau, Wyspy Marshalla, Ekwador

    [Display(Name = "Uruguayan Peso")]
    UYU,

    [Display(Name = "Uzbekistani Som")]
    UZS,

    // V
    [Display(Name = "Vanuatu Vatu")]
    VUV,

    [Display(Name = "Venezuelan Bolívar Soberano")]
    VES,

    [Display(Name = "Vietnamese Đồng")]
    VND,

    // Y
    [Display(Name = "Yemeni Rial")]
    YER,

    // Z
    [Display(Name = "Zambian Kwacha")]
    ZMW,

    [Display(Name = "Zimbabwean Dollar")]
    ZWL
}

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        // 1. Get enum value
        var member = enumValue.GetType().GetMember(enumValue.ToString()).First();

        // 2. Get [Display] value
        var displayAttribute = member.GetCustomAttribute<DisplayAttribute>();

        // 3. Return DisplayName
        return displayAttribute?.GetName() ?? enumValue.ToString();
    }
}
