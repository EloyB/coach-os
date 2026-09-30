const {chromium}=require("playwright-core");const path=require("path");const fs=require("fs");
const EXE=process.env.HOME+"/Library/Caches/ms-playwright/chromium_headless_shell-1243/chrome-headless-shell-mac-arm64/chrome-headless-shell";
const TONE=process.env.TONE||"appstore";
(async()=>{
 const dir="samples_"+TONE; fs.mkdirSync(dir,{recursive:true});
 const b=await chromium.launch({executablePath:EXE,args:["--force-color-profile=srgb"]});
 const p=await b.newPage({viewport:{width:1920,height:1080},deviceScaleFactor:1});
 await p.goto("file://"+path.join(__dirname,"video2.html")+"?tone="+TONE);
 await p.waitForFunction("window.__ready===true");await p.evaluate(()=>document.fonts.ready);
 await p.evaluate(()=>window.measure&&window.measure());await p.waitForTimeout(200);
 const times=[1.9,3.5,6.2,8.9,11.0,13.5,15.0,19.6];
 for(const t of times){await p.evaluate(t=>window.SEEK(t),t);await p.screenshot({path:dir+"/t"+String(t).replace(".","_")+".png"});}
 await b.close();console.log(TONE+" samples done");
})().catch(e=>{console.error(e);process.exit(1)});
