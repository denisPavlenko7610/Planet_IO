import argparse
import csv
import sys

from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

LOCALE_COLUMNS = {"NotoSansSC": "zh-Hans", "NotoSansJP": "ja", "NotoSansKR": "ko"}
LEGACY_CODECS = {
    "NotoSansSC": ("gb2312", [range(0xA1, 0xA4), range(0xB0, 0xD7)], range(0xA1, 0xFF)),
    "NotoSansJP": ("shift_jis", [range(0x81, 0x85), range(0x88, 0x99)], list(range(0x40, 0x7F)) + list(range(0x80, 0xFD))),
    "NotoSansKR": ("euc-kr", [range(0xA1, 0xA3), range(0xB0, 0xC9)], range(0xA1, 0xFF)),
}


def legacy_characters(name):
    codec, lead_ranges, trail_range = LEGACY_CODECS[name]
    characters = set()
    for lead_range in lead_ranges:
        for lead in lead_range:
            for trail in trail_range:
                try:
                    characters.add(bytes([lead, trail]).decode(codec))
                except UnicodeDecodeError:
                    continue
    if name == "NotoSansJP":
        for code in range(0xA1, 0xE0):
            characters.add(bytes([code]).decode(codec))
    return {character for character in characters if ord(character) > 0x7F}


def locale_characters(csv_path, column):
    with open(csv_path, encoding="utf-8", newline="") as handle:
        rows = list(csv.reader(handle))
    index = rows[0].index(column)
    return {character for row in rows[1:] if len(row) > index for character in row[index]}


def build(name, source, destination, csv_path, weight):
    characters = set(chr(code) for code in range(0x20, 0x7F))
    characters |= locale_characters(csv_path, LOCALE_COLUMNS[name])
    characters |= legacy_characters(name)
    characters.discard("\n")

    options = subset.Options()
    options.layout_features = ["*"]
    options.notdef_outline = True
    options.name_IDs = ["*"]
    options.glyph_names = False
    font = TTFont(source)
    subsetter = subset.Subsetter(options)
    subsetter.populate(unicodes=[ord(character) for character in characters])
    subsetter.subset(font)
    static = instantiateVariableFont(font, {"wght": weight}, inplace=False)
    static.save(destination)
    return len(characters)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--csv", required=True)
    parser.add_argument("--weight", type=int, default=700)
    arguments = parser.parse_args()

    for name in LOCALE_COLUMNS:
        count = build(
            name,
            f"{arguments.input}/{name}-VF.ttf",
            f"{arguments.output}/{name}-Bold.ttf",
            arguments.csv,
            arguments.weight,
        )
        print(f"{name}: {count} characters", file=sys.stderr)


if __name__ == "__main__":
    main()
