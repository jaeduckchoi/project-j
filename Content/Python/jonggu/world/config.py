"""Narrow, lossless updates to the project MapsSettings keys."""
import re


def _ini_values(text, values):
    """Change only named MapsSettings values, retaining unrelated bytes/newlines."""
    header = '[/Script/EngineSettings.GameMapsSettings]'
    match = re.search(r'^' + re.escape(header) + r'[ \t]*(?:\r?\n|$)', text, re.M)
    newline = '\r\n' if '\r\n' in text else '\n'
    if match is None:
        prefix = text + ('' if not text or text.endswith(('\n', '\r')) else newline)
        return prefix + header + newline + ''.join(k + '=' + v + newline for k, v in values.items())
    following = re.search(r'^\[', text[match.end():], re.M)
    end = match.end() + following.start() if following else len(text)
    body = text[match.end():end]
    for key, value in values.items():
        pattern = r'^([ \t]*' + re.escape(key) + r'[ \t]*=)[^\r\n]*'
        if re.search(pattern, body, re.M):
            body = re.sub(pattern, lambda m: m.group(1) + value, body, flags=re.M)
        else:
            body += ('' if not body or body.endswith(('\n', '\r')) else newline) + key + '=' + value + newline
    return text[:match.end()] + body + text[end:]
