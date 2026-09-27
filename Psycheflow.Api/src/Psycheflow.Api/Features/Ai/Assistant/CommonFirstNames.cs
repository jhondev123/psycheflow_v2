namespace Psycheflow.Api.Features.Ai.Assistant;

/// <summary>
/// Prenomes mais comuns no Brasil (base: listas do Censo IBGE), sem acento e em minúsculas, usados para mascarar
/// terceiros citados nas anotações (DT-26). Ficaram de fora nomes que também são palavras comuns
/// ("Rosa", "Luz", "Glória", "Graça", "Vitória", "Aurora", "Cruz"…) para não apagar conteúdo clínico.
/// </summary>
internal static class CommonFirstNames
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        // Femininos
        "maria", "ana", "francisca", "antonia", "adriana", "juliana", "marcia", "fernanda", "patricia", "aline",
        "sandra", "camila", "amanda", "bruna", "jessica", "leticia", "julia", "luciana", "vanessa", "mariana",
        "gabriela", "vera", "larissa", "claudia", "beatriz", "luana", "rita", "sonia", "renata", "eliane",
        "josefa", "simone", "natalia", "cristiane", "carla", "debora", "rosangela", "jaqueline", "daniela", "aparecida",
        "marlene", "terezinha", "tereza", "teresa", "raimunda", "andreia", "andrea", "fabiana", "lucia", "raquel",
        "angela", "rafaela", "joana", "luzia", "elaine", "daiane", "regina", "rosana", "tatiane", "kelly",
        "sabrina", "isabela", "isabella", "isabel", "helena", "alice", "laura", "manuela", "valentina", "sophia",
        "sofia", "heloisa", "lorena", "livia", "giovanna", "giovana", "eduarda", "yasmin", "lara", "cecilia",
        "clara", "luiza", "luisa", "fatima", "silvia", "priscila", "carolina", "caroline", "tatiana", "michele",
        "michelle", "viviane", "elisa", "elisangela", "edna", "ivone", "irene", "ines", "alessandra", "roberta",
        "bianca", "barbara", "thais", "tais", "paula", "paola", "carmen", "marta", "martha", "denise",
        "monica", "flavia", "gisele", "karina", "katia", "lais", "milena", "nathalia", "nicole", "olivia",
        "pietra", "rebeca", "sara", "sarah", "stella", "estela", "yara", "iara", "zilda", "neusa",
        "vilma", "odete", "dalva", "celia", "solange", "suely", "sueli", "rosemary", "rosimeire", "adriele",
        "agatha", "alana", "ester", "esther", "emanuelle", "emily", "evelyn", "geovana", "ingrid", "joyce",
        "jussara", "lidia", "lilian", "mirian", "miriam", "noemi", "rayssa", "samara", "selma", "tamires",
        "melissa", "antonella", "maite", "catarina", "aurelia",

        // Masculinos
        "jose", "joao", "antonio", "francisco", "carlos", "paulo", "pedro", "lucas", "luiz", "luis",
        "marcos", "gabriel", "rafael", "daniel", "marcelo", "bruno", "eduardo", "felipe", "raimundo", "rodrigo",
        "manoel", "manuel", "sebastiao", "geraldo", "severino", "fernando", "diego", "leandro", "marcio", "anderson",
        "alex", "alexandre", "andre", "fabio", "ricardo", "sergio", "jorge", "roberto", "miguel", "arthur",
        "artur", "heitor", "bernardo", "davi", "david", "theo", "teo", "lorenzo", "gustavo", "matheus",
        "mateus", "enzo", "guilherme", "nicolas", "samuel", "benjamin", "joaquim", "vinicius", "leonardo", "henrique",
        "murilo", "thiago", "tiago", "caio", "otavio", "igor", "vitor", "victor", "renan", "wellington",
        "wesley", "julio", "cicero", "edson", "adriano", "rogerio", "reinaldo", "robson", "claudio", "mauro",
        "mario", "hugo", "emanuel", "raul", "ivan", "joel", "nelson", "wagner", "valdir", "osvaldo",
        "benedito", "aparecido", "luan", "kaique", "ryan", "pietro", "caua", "isaac", "rian", "breno",
        "cristiano", "denis", "douglas", "elias", "everton", "fabricio", "flavio", "gilberto", "gilson", "helio",
        "jair", "jefferson", "jonas", "jonathan", "juliano", "kevin", "lauro", "leonel", "luciano", "marcelino",
        "mauricio", "milton", "moises", "nathan", "noah", "orlando", "otto", "renato", "ronaldo", "rubens",
        "silvio", "valter", "walter", "washington", "william", "yuri", "zeca", "anthony", "bento",
        "benicio", "gael", "ravi", "rodolfo", "tomas", "thomas", "vicente", "augusto", "cesar", "ernesto",
        "gregorio", "horacio", "inacio", "leopoldo", "saulo", "tadeu", "ulisses", "valdemar",
    };
}
