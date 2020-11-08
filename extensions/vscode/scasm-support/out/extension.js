"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.activate = void 0;
// based on https://github.com/microsoft/qsharp-compiler/blob/e1a43f3da021fc94d7214ea197ba3905ff96695c/src/VSCodeExtension/src/languageServer.ts
const fs = require("fs");
const cp = require("child_process");
const net = require("net");
const url = require("url");
const vscode = require("vscode");
const vscode_lc = require("vscode-languageclient");
function activate(context) {
    return __awaiter(this, void 0, void 0, function* () {
        console.log("SC extension activated");
        yield startLanguageServer(context);
    });
}
exports.activate = activate;
function startLanguageServer(context) {
    return __awaiter(this, void 0, void 0, function* () {
        const rootFolder = findRootFolder();
        const config = vscode.workspace.getConfiguration();
        const exePath = config.get("sclang.languageServerPath");
        if (!(yield isValidExe(exePath))) {
            console.log(`[sclang] sclang.languageServerPath = '${exePath}' is not an executable`);
            return;
        }
        const serverOptions = startServerOptions(exePath, rootFolder);
        const clientOptions = {
            documentSelector: [
                { scheme: "file", language: "sclang" }
            ],
            uriConverters: {
                // VS Code by default %-encodes even the colon after the drive letter
                code2Protocol: uri => url.format(url.parse(uri.toString(true))),
                protocol2Code: str => vscode.Uri.parse(str)
            },
            errorHandler: {
                closed: () => {
                    return vscode_lc.CloseAction.Restart;
                },
                error: (error, message, count) => {
                    console.log(`[sclang] Client error. ${error.name}: ${error.message}`);
                    // By default, continue the server as best as possible.
                    return vscode_lc.ErrorAction.Continue;
                }
            }
        };
        const client = new vscode_lc.LanguageClient("sclang", "ScLang", serverOptions, clientOptions, false);
        let disposable = client.start();
        context.subscriptions.push(disposable);
        console.log("[sclang] Started language client.");
    });
}
function startServerOptions(exePath, cwd) {
    return () => new Promise((resolve, reject) => {
        const server = net.createServer(socket => {
            resolve({ reader: socket, writer: socket });
        });
        let port = 8091;
        listenPromise(server, port, port + 10, '127.0.0.1')
            .then((actualPort) => {
            spawnProcess(exePath, cwd, port);
        })
            .catch(err => {
            reject(err);
        });
    });
}
function listenPromise(server, port, maxPort, hostname) {
    return new Promise((resolve, reject) => {
        if (port >= maxPort) {
            reject("Could not find port");
        }
        server.listen(port, hostname)
            .on("listening", () => resolve(port))
            .on("error", err => {
            if ("code" in err && err.code === "EADDRINUSE") {
                resolve(listenPromise(server, port + 1, maxPort, hostname));
            }
            reject(err);
        });
    });
}
function spawnProcess(exePath, cwd, port) {
    const process = cp.spawn(exePath, [`${port}`], { cwd: cwd })
        .on("error", err => {
        console.log(`[sclang] Language server process spawn failed with '${err}'`);
        throw err;
    })
        .on("exit", (code, signal) => console.log(`[sclang] Language server process exited with code ${code}`));
    process.stderr.on('data', (data) => {
        console.error(`[sclang-language-server] ${data}`);
    });
    process.stdout.on('data', (data) => {
        console.log(`[sclang-language-server] ${data}`);
    });
    console.log(`[sclang] Started language server process with PID ${process.pid}`);
}
function isValidExe(exePath) {
    return new Promise((resolve, reject) => {
        if (exePath === undefined || exePath === null) {
            resolve(false);
        }
        else {
            fs.access(exePath, fs.constants.X_OK, err => {
                resolve(err === null);
            });
        }
    });
}
function findRootFolder() {
    const workspaces = vscode.workspace.workspaceFolders;
    if (workspaces) {
        return workspaces[0].uri.fsPath;
    }
    else {
        return "";
    }
}
//# sourceMappingURL=extension.js.map