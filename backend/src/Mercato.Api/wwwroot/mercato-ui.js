(() => {
  const coreFa = {
    'Search…':'جستجو…','Previous':'قبلی','Next':'بعدی','Page':'صفحه','of':'از','rows':'ردیف',
    'Product':'محصول','Quantity':'تعداد','Sale price':'قیمت فروش','Purchase cost':'قیمت خرید','Total':'جمع',
    'Subtotal':'جمع قبل از تخفیف','Discount':'تخفیف','Customer':'مشتری','Payment method':'روش پرداخت',
    'Sold':'فروخته‌شده','Returned':'مرجوع‌شده','Remaining':'باقی‌مانده','Paid':'پرداخت‌شده','Unpaid':'پرداخت‌نشده',
    'Actions':'عملیات','No records found.':'رکوردی یافت نشد.','Assigned to all branches':'دسترسی به همه شعب'
  };
  let language = localStorage.getItem('mercato.language') || 'en';
  let locale = {};
  let applying = false;
  const originals = new WeakMap();
  const pad = n => String(n).padStart(2, '0');
  const t = value => {
    const key = String(value ?? '');
    return language === 'fa' ? (locale[key] || coreFa[key] || key) : key;
  };

  async function loadLocale() {
    try {
      const response = await fetch(`/locales/${language === 'fa' ? 'fa' : 'en'}.json`, { cache: 'no-cache' });
      locale = response.ok ? await response.json() : {};
    } catch { locale = {}; }
  }

  function injectStyle() {
    if (document.getElementById('mercato-ui-style')) return;
    const style = document.createElement('style');
    style.id = 'mercato-ui-style';
    style.textContent = `
      @font-face{font-family:Dana;src:url('/fonts/Dana-Regular.woff2') format('woff2');font-weight:400;font-display:swap}
      @font-face{font-family:Dana;src:url('/fonts/Dana-Medium.woff2') format('woff2');font-weight:500 600;font-display:swap}
      @font-face{font-family:Dana;src:url('/fonts/Dana-DemiBold.woff2') format('woff2');font-weight:700 900;font-display:swap}
      html[dir=rtl] body,html[dir=rtl] button,html[dir=rtl] input,html[dir=rtl] select,html[dir=rtl] textarea{font-family:Dana,Tahoma,Arial,sans-serif!important}
      .mcombo{position:relative;width:100%;min-width:0}.mcombo-native{position:absolute!important;opacity:0!important;pointer-events:none!important;width:1px!important;height:1px!important}
      .mcombo-button{width:100%;min-height:40px;padding:9px 36px 9px 12px;border:1px solid #cfd6de;border-radius:10px;background:#fff;color:inherit;text-align:start;position:relative;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
      [dir=rtl] .mcombo-button{padding:9px 12px 9px 36px}.mcombo-button:after{content:'▾';position:absolute;inset-inline-end:12px;top:50%;transform:translateY(-50%);opacity:.6}
      .mcombo-menu{display:none;position:absolute;z-index:5000;inset-inline:0;top:calc(100% + 5px);background:#fff;border:1px solid #d8dee5;border-radius:11px;box-shadow:0 16px 42px rgba(20,30,40,.16);padding:7px;min-width:220px;max-height:min(350px,calc(100vh - 24px))}.mcombo.open .mcombo-menu{display:block}
      .mcombo-search{width:100%;border:1px solid #d7dde4;border-radius:8px;padding:8px 10px;margin-bottom:6px;background:#fff}.mcombo-options{max-height:min(260px,calc(100vh - 110px));overflow:auto}.mcombo-option{display:block;width:100%;border:0;background:transparent;text-align:start;padding:8px 9px;border-radius:7px;color:#263442}.mcombo-option:hover,.mcombo-option.selected{background:#edf4f1}.mcombo-option.disabled{opacity:.45;pointer-events:none}.mcombo-empty{padding:10px;color:#7b8793;font-size:12px}
      .persian-date-shell{position:relative;width:100%;min-width:0}.persian-date-input{width:100%!important;min-width:0;direction:ltr!important;text-align:left!important}.persian-calendar{position:absolute;z-index:6000;inset-inline-start:0;top:calc(100% + 5px);width:min(310px,calc(100vw - 32px));max-height:min(430px,calc(100vh - 24px));overflow:auto;background:#fff;border:1px solid #d8dee5;border-radius:13px;box-shadow:0 18px 46px rgba(20,30,40,.18);padding:12px;direction:rtl;color:#17202a}
      .pc-head{display:flex;justify-content:space-between;align-items:center;gap:8px;margin-bottom:9px}.pc-head button{border:0;background:#edf1f4;border-radius:7px;padding:6px 10px}.pc-title{font-weight:800}.pc-week,.pc-days{display:grid;grid-template-columns:repeat(7,1fr);gap:3px;text-align:center}.pc-week span{font-size:11px;color:#74808c;padding:5px}.pc-day{border:0;background:transparent;border-radius:8px;padding:7px 2px}.pc-day:hover{background:#edf4f1}.pc-day.selected{background:#2d5b4f;color:#fff}.pc-time{display:flex;align-items:center;gap:6px;margin-top:10px}.pc-time input{width:64px;border:1px solid #d7dde4;border-radius:8px;padding:6px;text-align:center}.pc-actions{display:flex;justify-content:space-between;gap:6px;margin-top:10px}.pc-actions button{border:0;border-radius:8px;padding:7px 10px}.pc-primary{background:#2d5b4f;color:#fff}.pc-secondary{background:#edf1f4}
    `;
    document.head.appendChild(style);
  }

  function translateText(node) {
    if (!originals.has(node)) originals.set(node, node.nodeValue);
    const original = originals.get(node) || '';
    const trimmed = original.trim();
    if (trimmed) node.nodeValue = original.replace(trimmed, t(trimmed));
  }
  function translateElement(el) {
    if (!el) return;
    for (const attr of ['placeholder','title','aria-label']) {
      if (!el.hasAttribute?.(attr)) continue;
      const store = `data-i18n-${attr}`;
      if (!el.hasAttribute(store)) el.setAttribute(store, el.getAttribute(attr) || '');
      el.setAttribute(attr, t(el.getAttribute(store)));
    }
    for (const child of el.childNodes || []) {
      if (child.nodeType === Node.TEXT_NODE) translateText(child);
      else if (child.nodeType === Node.ELEMENT_NODE) translateElement(child);
    }
  }

  function positionPopup(anchor, popup) {
    if (!anchor || !popup) return;
    popup.style.top = 'calc(100% + 5px)'; popup.style.bottom = 'auto';
    requestAnimationFrame(() => {
      const rect = anchor.getBoundingClientRect();
      const desired = Math.min(popup.scrollHeight || 320, 430);
      if (window.innerHeight - rect.bottom < desired && rect.top > desired) {
        popup.style.top = 'auto'; popup.style.bottom = 'calc(100% + 5px)';
      }
    });
  }

  function closeAllCombos() { document.querySelectorAll('.mcombo.open').forEach(x => x.classList.remove('open')); }
  function renderComboOptions(wrap) {
    const select = wrap.querySelector('select');
    const search = wrap.querySelector('.mcombo-search');
    const list = wrap.querySelector('.mcombo-options');
    const q = (search.value || '').trim().toLocaleLowerCase(language === 'fa' ? 'fa-IR' : 'en-US');
    const options = [...select.options].filter(o => !q || o.text.toLocaleLowerCase(language === 'fa' ? 'fa-IR' : 'en-US').includes(q));
    list.innerHTML = options.length ? options.map(o => `<button type="button" class="mcombo-option ${o.selected ? 'selected' : ''} ${o.disabled ? 'disabled' : ''}" data-value="${escapeHtml(o.value)}">${escapeHtml(o.text)}</button>`).join('') : `<div class="mcombo-empty">${escapeHtml(t('No records found.'))}</div>`;
    list.querySelectorAll('.mcombo-option').forEach(button => button.onclick = () => {
      select.value = button.dataset.value;
      select.dispatchEvent(new Event('change', { bubbles:true }));
      syncCombo(wrap); closeAllCombos();
    });
  }
  function syncCombo(target) {
    const wrap = target?.classList?.contains('mcombo') ? target : target?.closest?.('.mcombo');
    if (!wrap) return;
    const select = wrap.querySelector('select'), button = wrap.querySelector('.mcombo-button');
    if (!select || !button) return;
    button.textContent = select.selectedOptions?.[0]?.text || select.options?.[0]?.text || '';
    if (wrap.classList.contains('open')) renderComboOptions(wrap);
  }
  function enhanceSelects(root=document) {
    const selects = [];
    if (root.matches?.('select:not(.no-search)')) selects.push(root);
    root.querySelectorAll?.('select:not(.no-search)').forEach(x => selects.push(x));
    for (const select of selects) {
      if (select.dataset.combo === '1') { syncCombo(select.closest('.mcombo')); continue; }
      select.dataset.combo = '1'; select.classList.add('mcombo-native');
      const wrap = document.createElement('div'); wrap.className = 'mcombo';
      select.parentNode.insertBefore(wrap, select); wrap.appendChild(select);
      const button = document.createElement('button'); button.type='button'; button.className='mcombo-button'; wrap.appendChild(button);
      const menu = document.createElement('div'); menu.className='mcombo-menu';
      const search = document.createElement('input'); search.type='search'; search.className='mcombo-search'; search.placeholder=t('Search…');
      const list = document.createElement('div'); list.className='mcombo-options';
      menu.append(search, list); wrap.appendChild(menu);
      button.onclick = () => { const was = wrap.classList.contains('open'); closeAllCombos(); if (!was) { wrap.classList.add('open'); search.value=''; renderComboOptions(wrap); positionPopup(wrap, menu); setTimeout(() => search.focus(), 0); } };
      search.oninput = () => renderComboOptions(wrap);
      select.addEventListener('change', () => syncCombo(wrap));
      syncCombo(wrap);
    }
  }
  document.addEventListener('click', e => { if (!e.target.closest?.('.mcombo')) closeAllCombos(); });
  const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));

  function div(a,b){return Math.trunc(a/b)}
  function mod(a,b){return a-Math.trunc(a/b)*b}
  const breaks=[-61,9,38,199,426,686,756,818,1111,1181,1210,1635,2060,2097,2192,2262,2324,2394,2456,3178];
  function jalCal(jy){let bl=breaks.length,gy=jy+621,leapJ=-14,jp=breaks[0],jm=0,jump=0,leap,leapG,march,n,i;if(jy<jp||jy>=breaks[bl-1])throw new Error('Invalid Jalaali year');for(i=1;i<bl;i++){jm=breaks[i];jump=jm-jp;if(jy<jm)break;leapJ+=div(jump,33)*8+div(mod(jump,33),4);jp=jm}n=jy-jp;leapJ+=div(n,33)*8+div(mod(n,33)+3,4);if(mod(jump,33)===4&&jump-n===4)leapJ++;leapG=div(gy,4)-div((div(gy,100)+1)*3,4)-150;march=20+leapJ-leapG;if(jump-n<6)n=n-jump+div(jump+4,33)*33;leap=mod(mod(n+1,33)-1,4);if(leap===-1)leap=4;return{leap,gy,march}}
  function g2d(gy,gm,gd){let d=div((gy+div(gm-8,6)+100100)*1461,4)+div(153*mod(gm+9,12)+2,5)+gd-34840408;d=d-div(div(gy+100100+div(gm-8,6),100)*3,4)+752;return d}
  function d2g(jdn){let j=4*jdn+139361631;j=j+div(div(4*jdn+183187720,146097)*3,4)*4-3908;const i=div(mod(j,1461),4)*5+308;const gd=div(mod(i,153),5)+1,gm=mod(div(i,153),12)+1,gy=div(j,1461)-100100+div(8-gm,6);return{gy,gm,gd}}
  function j2d(jy,jm,jd){const r=jalCal(jy);return g2d(r.gy,3,r.march)+(jm-1)*31-div(jm,7)*(jm-7)+jd-1}
  function d2j(jdn){const g=d2g(jdn),jy=g.gy-621,r=jalCal(jy),jdn1f=g2d(g.gy,3,r.march);let k=jdn-jdn1f;if(k>=0){if(k<=185)return{jy,jm:1+div(k,31),jd:mod(k,31)+1};k-=186}else{const jy2=jy-1;k+=179;if(r.leap===1)k++;return{jy:jy2,jm:7+div(k,30),jd:mod(k,30)+1}}return{jy,jm:7+div(k,30),jd:mod(k,30)+1}}
  function gregorianToJalaliDateTime(value){if(!value)return'';const d=new Date(value),j=d2j(g2d(d.getFullYear(),d.getMonth()+1,d.getDate()));return`${j.jy}/${pad(j.jm)}/${pad(j.jd)} ${pad(d.getHours())}:${pad(d.getMinutes())}`}
  function jalaliToLocalDateTime(value){const m=String(value||'').trim().match(/^(\d{4})[\/-](\d{1,2})[\/-](\d{1,2})(?:\s+(\d{1,2}):(\d{2}))?$/);if(!m)return null;try{const month=+m[2],day=+m[3];if(month<1||month>12||day<1||day>daysInJalaliMonth(+m[1],month))return null;const g=d2g(j2d(+m[1],month,day));return`${g.gy}-${pad(g.gm)}-${pad(g.gd)}T${pad(+(m[4]||0))}:${pad(+(m[5]||0))}`}catch{return null}}
  const monthNames=['فروردین','اردیبهشت','خرداد','تیر','مرداد','شهریور','مهر','آبان','آذر','دی','بهمن','اسفند'];
  function daysInJalaliMonth(y,m){if(m<=6)return 31;if(m<=11)return 30;return jalCal(y).leap===0?30:29}

  function syncDateInputs(root=document) {
    const inputs=[];
    if (root.matches?.('input[type="datetime-local"]')) inputs.push(root);
    root.querySelectorAll?.('input[type="datetime-local"]').forEach(x => inputs.push(x));
    for (const input of inputs) {
      if (input.dataset.persian === '1') { toggleDateMode(input, input.closest('.persian-date-shell')); continue; }
      input.dataset.persian='1';
      const shell=document.createElement('div'); shell.className='persian-date-shell'; input.parentNode.insertBefore(shell,input); shell.appendChild(input);
      const alt=document.createElement('input'); alt.type='text'; alt.className=`${input.className||''} persian-date-input`; alt.placeholder='1405/06/12 14:30'; shell.appendChild(alt);
      const open=()=>openPersianCalendar(input,alt,shell); alt.addEventListener('focus',open); alt.addEventListener('click',open);
      alt.addEventListener('change',()=>{const v=jalaliToLocalDateTime(alt.value);if(v){input.value=v;input.dispatchEvent(new Event('change',{bubbles:true}))}});
      input.addEventListener('change',()=>alt.value=gregorianToJalaliDateTime(input.value));
      toggleDateMode(input,shell);
    }
  }
  function toggleDateMode(input,shell){if(!shell)return;const alt=shell.querySelector('.persian-date-input');if(!alt)return;if(language==='fa'){alt.value=gregorianToJalaliDateTime(input.value);input.style.display='none';alt.style.display='block'}else{input.style.display='';alt.style.display='none';shell.querySelector('.persian-calendar')?.remove()}}
  function openPersianCalendar(input,alt,shell){
    if(language!=='fa')return;
    document.querySelectorAll('.persian-calendar').forEach(x=>x.remove());
    let base=input.value?new Date(input.value):new Date();if(Number.isNaN(base.getTime()))base=new Date();
    const j=d2j(g2d(base.getFullYear(),base.getMonth()+1,base.getDate()));
    const state={y:j.jy,m:j.jm,d:j.jd,h:base.getHours(),min:base.getMinutes()};
    const cal=document.createElement('div');cal.className='persian-calendar';shell.appendChild(cal);
    const render=()=>{
      const firstG=d2g(j2d(state.y,state.m,1));const first=new Date(firstG.gy,firstG.gm-1,firstG.gd);const offset=(first.getDay()+1)%7;const count=daysInJalaliMonth(state.y,state.m);
      state.d=Math.min(state.d,count);
      let days='';for(let i=0;i<offset;i++)days+='<span></span>';for(let d=1;d<=count;d++)days+=`<button type="button" class="pc-day ${d===state.d?'selected':''}" data-day="${d}">${d.toLocaleString('fa-IR')}</button>`;
      cal.innerHTML=`<div class="pc-head"><button type="button" data-prev>‹</button><span class="pc-title">${monthNames[state.m-1]} ${state.y.toLocaleString('fa-IR',{useGrouping:false})}</span><button type="button" data-next>›</button></div><div class="pc-week"><span>ش</span><span>ی</span><span>د</span><span>س</span><span>چ</span><span>پ</span><span>ج</span></div><div class="pc-days">${days}</div><div class="pc-time"><span>ساعت</span><input data-hour type="number" min="0" max="23" value="${state.h}"><span>:</span><input data-minute type="number" min="0" max="59" value="${state.min}"></div><div class="pc-actions"><button type="button" class="pc-secondary" data-today>امروز</button><button type="button" class="pc-primary" data-apply>تایید</button></div>`;
      cal.querySelector('[data-prev]').onclick=()=>{state.m--;if(state.m<1){state.m=12;state.y--}render()};
      cal.querySelector('[data-next]').onclick=()=>{state.m++;if(state.m>12){state.m=1;state.y++}render()};
      cal.querySelectorAll('[data-day]').forEach(b=>b.onclick=()=>{state.d=+b.dataset.day;render()});
      cal.querySelector('[data-today]').onclick=()=>{const now=new Date(),jj=d2j(g2d(now.getFullYear(),now.getMonth()+1,now.getDate()));Object.assign(state,{y:jj.jy,m:jj.jm,d:jj.jd,h:now.getHours(),min:now.getMinutes()});render()};
      cal.querySelector('[data-apply]').onclick=()=>{state.h=Math.max(0,Math.min(23,+cal.querySelector('[data-hour]').value||0));state.min=Math.max(0,Math.min(59,+cal.querySelector('[data-minute]').value||0));alt.value=`${state.y}/${pad(state.m)}/${pad(state.d)} ${pad(state.h)}:${pad(state.min)}`;const v=jalaliToLocalDateTime(alt.value);if(v){input.value=v;input.dispatchEvent(new Event('change',{bubbles:true}))}cal.remove()};
      positionPopup(shell,cal);
    };
    render();
  }

  function fmtDate(value){if(!value)return'—';const d=new Date(value);return new Intl.DateTimeFormat(language==='fa'?'fa-IR-u-ca-persian':'en-US',{year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit'}).format(d)}
  function money(value){return Number(value||0).toLocaleString(language==='fa'?'fa-IR':'en-US',{minimumFractionDigits:2,maximumFractionDigits:2})}
  async function apply(root=document){if(applying)return;applying=true;try{injectStyle();await loadLocale();document.documentElement.lang=language;document.documentElement.dir=language==='fa'?'rtl':'ltr';translateElement(root.documentElement||root);enhanceSelects(root);syncDateInputs(root);document.querySelectorAll('.mcombo').forEach(syncCombo)}finally{applying=false}}
  async function setLanguage(value){language=value==='fa'?'fa':'en';localStorage.setItem('mercato.language',language);closeAllCombos();await apply();document.dispatchEvent(new CustomEvent('mercato:language',{detail:{language}}))}
  function getLanguage(){return language}

  const observer=new MutationObserver(records=>{if(applying)return;let changed=false;for(const r of records)for(const n of r.addedNodes){if(n.nodeType===1||n.nodeType===3)changed=true}if(changed){enhanceSelects(document);syncDateInputs(document)}});
  document.addEventListener('DOMContentLoaded',async()=>{await apply();observer.observe(document.body,{childList:true,subtree:true})});
  window.MercatoUI={t,setLanguage,getLanguage,apply,fmtDate,money,enhanceSelects,syncCombo,jalaliToLocalDateTime,gregorianToJalaliDateTime};
})();