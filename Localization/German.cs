using System.Collections.Generic;
using DooDesch.Localization;

namespace RVRepairVan.Localization
{
    /// <summary>
    /// German translation table. Keys are the English source strings as they appear in code
    /// (gettext style, see DooDesch.Localization.L10n) - changing an English literal there
    /// requires updating its key here, or that line silently falls back to English.
    /// </summary>
    internal static class German
    {
        internal static void Register()
        {
            L10n.Register("de", new Dictionary<string, string>
            {
                // Quest journal
                ["Back on the Road"] = "Wieder auf Achse",
                ["Your RV's wrecked. Someone in Hyland Point has to know a guy."] =
                    "Dein Wohnmobil ist Schrott. Irgendwer in Hyland Point kennt doch sicher jemanden.",

                // Objectives
                ["Find a way to repair your RV"] = "Finde einen Weg, dein Wohnmobil zu reparieren",
                ["Ask the motel manager about the RV"] = "Frag die Motel-Managerin nach dem Wohnmobil",
                ["Talk to Mrs. Ming at the Chinese restaurant"] = "Sprich mit Mrs. Ming im Chinarestaurant",
                ["Pick up Ming's crate from the dead drop"] = "Hol Mings Kiste am Toten Briefkasten ab",
                ["Bring Ming's crate back to Mrs. Ming"] = "Bring Mings Kiste zurück zu Mrs. Ming",
                ["Talk to Marco at the body shop"] = "Sprich mit Marco in der Werkstatt",
                ["Tell Marco Mrs. Ming sent you"] = "Sag Marco, dass Mrs. Ming dich schickt",
                ["Pick up Marco's package from the dead drop"] = "Hol Marcos Paket am Toten Briefkasten ab",
                ["Bring Marco's package back"] = "Bring Marcos Paket zurück",
                ["Pay Marco for the repair"] = "Bezahl Marco für die Reparatur",
                ["Check on the RV"] = "Sieh nach dem Wohnmobil",

                // Donna
                ["My RV got blown up. Know anyone who can fix it?"] =
                    "Mein Wohnmobil wurde in die Luft gejagt. Kennst du jemanden, der es reparieren kann?",
                ["Do I look like a mechanic, sweetheart? Go ask Mrs. Ming over at the Chinese place. She knows people."] =
                    "Seh ich aus wie ein Mechaniker, Schätzchen? Frag Mrs. Ming drüben beim Chinesen. Die kennt Leute.",

                // Ming
                ["Donna said you might know someone who can fix my RV."] =
                    "Donna meinte, du kennst vielleicht jemanden, der mein Wohnmobil reparieren kann.",
                ["Marco at the docks can fix almost anything. But favors move both ways. I have a crate waiting at a dead drop nearby. Bring it back, and I'll put in a word."] =
                    "Marco am Hafen kann fast alles reparieren. Aber Gefallen beruhen auf Gegenseitigkeit. In einem Toten Briefkasten in der Nähe wartet eine Kiste auf mich. Bring sie her, und ich lege ein gutes Wort ein.",
                ["I'll grab it."] = "Ich hole sie.",
                ["Not right now."] = "Jetzt gerade nicht.",
                ["Good. Pick it up, bring it here, and don't open it."] =
                    "Gut. Hol sie ab, bring sie her, und mach sie nicht auf.",
                ["Then your RV can stay where it is."] = "Dann bleibt dein Wohnmobil eben, wo es ist.",
                ["Here's your crate."] = "Hier ist deine Kiste.",
                ["Good. Go see Marco at the body shop down by the docks. Tell him Mrs. Ming sent you."] =
                    "Gut. Geh zu Marco in der Werkstatt unten am Hafen. Sag ihm, Mrs. Ming schickt dich.",
                ["I lost your crate."] = "Ich habe deine Kiste verloren.",
                ["You lost it? I don't lose things, and people who lose my things lose teeth. Five hundred buys you both back. Now."] =
                    "Du hast sie verloren? Ich verliere nichts, und wer meine Sachen verliert, verliert Zähne. Fünfhundert bringen dich wieder ins Reine. Sofort.",
                ["Smart. We're square. Now go see Marco at the body shop down by the docks, and tell him Mrs. Ming sent you."] =
                    "Klug. Wir sind quitt. Jetzt geh zu Marco in der Werkstatt unten am Hafen und sag ihm, Mrs. Ming schickt dich.",
                ["Then don't come back until your hands are full."] =
                    "Dann komm erst wieder, wenn deine Hände voll sind.",

                // Marco
                ["Can you fix my RV?"] = "Kannst du mein Wohnmobil reparieren?",
                ["Yeah, I can fix it. Fifty grand."] = "Klar kann ich das reparieren. Fünfzig Riesen.",
                ["Fifty grand?"] = "Fünfzig Riesen?",
                ["You brought me a burnt-out shell. That's not a repair, that's a resurrection."] =
                    "Du bringst mir ein ausgebranntes Wrack. Das ist keine Reparatur, das ist eine Wiederauferstehung.",
                ["Mrs. Ming sent me."] = "Mrs. Ming schickt mich.",
                ["Mrs. Ming sent you? Yeah, alright. Should've opened with that. Ten grand."] =
                    "Mrs. Ming schickt dich? Na gut. Damit hättest du anfangen sollen. Zehn Riesen.",
                ["Repair my RV ({0})"] = "Mein Wohnmobil reparieren ({0})",
                ["Anything I can do to bring the price down?"] = "Kann ich irgendwas tun, um den Preis zu drücken?",
                ["Maybe. I left a package at a dead drop nearby. Pick it up, bring it back, and don't make it weird."] =
                    "Vielleicht. Ich habe ein Paket an einem Toten Briefkasten in der Nähe hinterlegt. Hol es ab, bring es her, und mach es nicht komisch.",
                ["Got your package."] = "Hab dein Paket.",
                ["Good. You can follow instructions. Bring me some of that good stuff now and then, and I'll keep shaving down the bill."] =
                    "Gut. Du kannst Anweisungen folgen. Bring mir ab und zu was von dem guten Zeug, dann drücke ich die Rechnung weiter.",
                ["I lost your package."] = "Ich habe dein Paket verloren.",
                ["You did what? You walk in here empty-handed and waste my time. Five hundred, or the next thing that goes missing is you."] =
                    "Du hast WAS? Du kommst mit leeren Händen rein und verschwendest meine Zeit. Fünfhundert, oder das Nächste, das verschwindet, bist du.",
                ["Good. Mess like that gets forgotten when the cash shows up. Bring me some of that good stuff now and then, and I'll keep shaving down the bill."] =
                    "Gut. Solche Ausrutscher sind vergessen, sobald das Geld da ist. Bring mir ab und zu was von dem guten Zeug, dann drücke ich die Rechnung weiter.",
                ["Clock's running. Come back with it."] = "Die Uhr läuft. Komm mit dem Ding zurück.",
                ["Your RV looks fine to me."] = "Dein Wohnmobil sieht für mich in Ordnung aus.",
                ["You're short. Come back when you've got the cash."] =
                    "Zu wenig. Komm wieder, wenn du das Geld hast.",
                ["Alright. Hold still, this won't take long."] = "Na gut. Bleib stehen, dauert nicht lange.",
                ["There she is - back from the dead. Go take a look, and try not to total her again."] =
                    "Da ist sie - zurück von den Toten. Sieh sie dir an, und versuch, sie nicht gleich wieder zu schrotten.",
                ["There she is - back from the dead. Try not to total her again."] =
                    "Da ist sie - zurück von den Toten. Versuch, sie nicht gleich wieder zu schrotten.",
                ["There she is. Standing again. Interior's your problem. Try not to piss off whoever torched it the first time."] =
                    "Da ist sie. Steht wieder. Der Innenraum ist dein Problem. Und verärgere besser nicht nochmal den, der sie abgefackelt hat.",

                // Samples
                ["Give Marco a packaged sample"] = "Marco eine verpackte Probe geben",
                ["Give Marco: {0} (-{1})"] = "Marco geben: {0} (-{1})",
                ["product"] = "Ware",
                ["That ain't packaged. Hand me something sealed."] =
                    "Das ist nicht verpackt. Gib mir was Versiegeltes.",
                ["Appreciate it. Knocked {0} off the bill."] =
                    "Danke dir. {0} gehen von der Rechnung runter.",
                ["What can I bring to lower the price?"] = "Was kann ich bringen, um den Preis zu senken?",
                ["Bring me packaged product - sealed stuff, not raw. Every piece I take knocks its value off the bill, up to five hundred a pop, right down to my floor."] =
                    "Bring mir verpackte Ware - versiegeltes Zeug, nichts Loses. Jedes Stück, das ich nehme, geht mit seinem Wert von der Rechnung ab, bis fünfhundert pro Stück, runter bis zu meiner Schmerzgrenze.",

                // Loss-fee sub-choices
                ["Pay ${0}"] = "{0} $ zahlen",
                ["I'll get the money."] = "Ich besorge das Geld.",

                // Marco's RV build-outs (each one paid for, each one with its own cinematic)
                ["Gut the interior. ({0})"] = "Reiß den Innenraum raus. ({0})",
                ["Build me a workshop floor. ({0})"] = "Bau mir einen Werkstattboden. ({0})",
                ["Make room for a crew. ({0})"] = "Mach Platz für eine Crew. ({0})",
                ["I need a loading dock. ({0})"] = "Ich brauche eine Laderampe. ({0})",
                ["Stripped her out. Bed, bench, the lot - it's a box on wheels now. Do something with it."] =
                    "Alles raus. Bett, Bank, der ganze Kram - jetzt ist es eine Kiste auf Rädern. Mach was draus.",
                ["Floor's braced and levelled. Bolt whatever you want to it, I don't want to know what."] =
                    "Boden ist verstärkt und gerade. Schraub drauf, was du willst, ich will es gar nicht wissen.",
                ["She'll hold a crew now. Put a locker in there or they'll stand around looking at you."] =
                    "Da passt jetzt eine Crew rein. Stell einen Spind rein, sonst stehen sie nur rum und gucken dich an.",
                ["Dock's in. Tell your supplier to bring it here and stop making you drive."] =
                    "Rampe steht. Sag deinem Lieferanten, er soll herkommen, statt dich fahren zu lassen.",
                ["Come back when you've got {0}."] = "Komm wieder, wenn du {0} hast.",

                // Repair-takes-a-day option
                ["Leave her with me. Come back tomorrow and I'll text you when she's done."] =
                    "Lass sie hier. Komm morgen wieder, ich schreib dir, wenn sie fertig ist.",
                ["She's done. Come get her whenever - and try not to total her again."] =
                    "Sie ist fertig. Hol sie ab, wann du willst - und fahr sie nicht gleich wieder zu Schrott.",

                // Quest items
                ["Ming's Crate"] = "Mings Kiste",
                ["A sealed crate for Mrs. Ming. She said not to open it."] =
                    "Eine versiegelte Kiste für Mrs. Ming. Sie hat gesagt: nicht öffnen.",
                ["Marco's Package"] = "Marcos Paket",
                ["A package Marco left at a drop. Don't make it weird."] =
                    "Ein Paket, das Marco an einem Briefkasten hinterlegt hat. Mach es nicht komisch.",
            });
        }
    }
}
