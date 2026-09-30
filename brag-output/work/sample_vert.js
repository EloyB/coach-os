const {chromium}=require("playwright-core");const path=require("path");const fs=require("fs");
const EXE=process.env.HOME+"/Library/Caches/ms-playwright/chromium_headless_shell-1243/chrome-headless-shell-mac-arm64/chrome-headless-shell";
(async()=>{
 fs.mkdirSync("samples_vert",{recursive:true});
 const b=await chromium.launch({executablePath:EXE,args:["--force-color-profile=srgb"]});
 const p=await b.newPage({viewport:{width:1080,height:1920},deviceScaleFactor:1});
 await p.goto("file://"+path.join(__dirname,"video_vert.html"));
 await p.waitForFunction("window.__ready===true");await p.evaluate(()=>document.fonts.ready);
 await p.evaluate(()=>window.measure&&window.measure());await p.waitForTimeout(200);
 for(const t of [1.9,6.2,8.9,11.5,15.0,19.6]){await p.evaluate(t=>window.SEEK(t),t);await p.screenshot({path:"samples_vert/t"+String(t).replace(".","_")+".png"});}
 await b.close();console.log("vert samples done");
})().catch(e=>{console.error(e);process.exit(1)});
