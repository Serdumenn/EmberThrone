#!/usr/bin/env python3
"""Minimal command-line client for the MCP for Unity bridge (http://localhost:8080/mcp).

For sessions that started before the editor was running: MCP clients load their tools at startup,
so such a session can still drive Unity through this script. Standard library only.

  python tools/unity_mcp.py --list                          list tool names
  python tools/unity_mcp.py --describe manage_editor        print one tool's input schema
  python tools/unity_mcp.py read_console '{"count": 20}'    call a tool with JSON arguments
  python tools/unity_mcp.py manage_camera '{"action":"screenshot","include_image":true}' --save shot.png

--save writes the first image in the result to a file instead of printing its base64.
"""
import base64
import json
import sys
import urllib.error
import urllib.request

URL = "http://localhost:8080/mcp"


def _read(resp):
    """The server answers with JSON or with a server-sent event stream; return the JSON-RPC reply."""
    body = resp.read().decode("utf-8")
    if "text/event-stream" not in (resp.headers.get("Content-Type") or ""):
        return json.loads(body) if body.strip() else None
    reply = None
    for line in body.splitlines():
        if line.startswith("data:"):
            data = line[5:].strip()
            if data:
                msg = json.loads(data)
                if "result" in msg or "error" in msg:
                    reply = msg
    return reply


class Client:
    def __init__(self):
        self.session = None
        self.next_id = 1
        init = self.rpc("initialize", {
            "protocolVersion": "2025-03-26",
            "capabilities": {},
            "clientInfo": {"name": "emberthrone-cli", "version": "1.0"},
        })
        if init is None or "error" in init:
            raise RuntimeError("initialize failed: " + json.dumps(init))
        self.rpc("notifications/initialized", {}, notify=True)

    def rpc(self, method, params, notify=False, timeout=300):
        msg = {"jsonrpc": "2.0", "method": method, "params": params}
        if not notify:
            msg["id"] = self.next_id
            self.next_id += 1
        headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        if self.session:
            headers["Mcp-Session-Id"] = self.session
        req = urllib.request.Request(URL, json.dumps(msg).encode("utf-8"), headers)
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            self.session = resp.headers.get("Mcp-Session-Id") or self.session
            return None if notify else _read(resp)


def main(argv):
    save = None
    if "--save" in argv:
        i = argv.index("--save")
        save = argv[i + 1]
        del argv[i:i + 2]
    if not argv:
        print(__doc__)
        return 2
    try:
        client = Client()
    except (urllib.error.URLError, ConnectionError, RuntimeError) as e:
        print("Bridge not reachable at " + URL + " (is the Unity editor open?): " + str(e), file=sys.stderr)
        return 3

    if argv[0] in ("--list", "--describe"):
        tools = client.rpc("tools/list", {})["result"]["tools"]
        if argv[0] == "--list":
            print("\n".join(t["name"] for t in tools))
            return 0
        print(json.dumps(next(t for t in tools if t["name"] == argv[1]), indent=2, ensure_ascii=False))
        return 0

    args = json.loads(argv[1]) if len(argv) > 1 else {}
    reply = client.rpc("tools/call", {"name": argv[0], "arguments": args})
    if reply is None or "error" in reply:
        print(json.dumps(reply, indent=2, ensure_ascii=False))
        return 1
    result = reply["result"]
    for item in result.get("content", []):
        if item.get("type") == "image" and save:
            with open(save, "wb") as f:
                f.write(base64.b64decode(item["data"]))
            print("saved " + save)
        elif item.get("type") == "image":
            print("<image, " + str(len(item.get("data", ""))) + " base64 chars; use --save>")
        else:
            print(item.get("text", json.dumps(item, ensure_ascii=False)))
    return 1 if result.get("isError") else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
