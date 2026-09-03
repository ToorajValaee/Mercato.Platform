(() => {
  const isAdmin = location.pathname.startsWith('/admin');
  const isPos = location.pathname.startsWith('/pos');
  const $ = id => document.getElementById(id);
  const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const state = { locale:{}, publicSettings:{ useUsername:false, systemLanguage:'en' }, staff:[], branches:[] };

  const style = document.createElement('style');
  style.textContent = `
    .lang{display:none!important}.panel{overflow:visible!important}.field-help{font-size:11px;color:#65717f;font-weight:400;line-height:1.6}.identity-hidden{display:none!important}
    .media-preview{display:flex;align-items:center;gap:10px;margin-top:8px}.media-preview img{width:72px;height:72px;object-fit:cover;border:1px solid #dde2e8;border-radius:10px}
    .print-powered{text-align:center;margin-top:28px;padding-top:10px;border-top:1px solid #ddd;color:#66736d;font:12px Arial,sans-serif}.print-powered a{color:inherit;text-decoration:none;font-weight:700}
    #loginView.mercato-login-art{max-width:none!important;width:100%!important;margin:0!important;min-height:100vh;display:grid!important;grid-template-columns:minmax(360px,1.15fr) minmax(360px,.85fr);align-items:stretch;padding:0!important;border:0!important;border-radius:0!important;background:#eef3f0!important;box-shadow:none!important}
    #loginView.mercato-login-art>.login-hero{min-height:100vh;background:center/cover no-repeat url('/admin/login-hero.svg')}
    #loginView.mercato-login-art>.login-card,#loginView.mercato-login-art>form{align-self:center;justify-self:center;width:min(440px,calc(100% - 40px));margin:24px;box-shadow:0 18px 50px rgba(20,40,33,.12)}
    #loginView.mercato-pos-login{max-width:none!important;width:100%!important;margin:0!important;min-height:100vh;display:grid!important;grid-template-columns:minmax(380px,1.2fr) minmax(360px,.8fr);padding:0!important;border:0!important;border-radius:0!important;background:#eef3f0!important;box-shadow:none!important}
    #loginView.mercato-pos-login>.login-hero{min-height:100vh;background:center/cover no-repeat url('/pos/login-hero.svg')}
    #loginView.mercato-pos-login>.login-form-shell{align-self:center;justify-self:center;width:min(440px,calc(100% - 40px));background:#fff;border:1px solid #e4e4e7;border-radius:18px;padding:28px;box-shadow:0 18px 50px rgba(20,40,33,.12)}
    #page-settlements .form-grid{align-items:start}#page-settlements .persian-date-shell{width:100%;min-width:0}#page-settlements .persian-date-input{width:100%!important;max-width:100%}
    @media(max-width:850px){#loginView.mercato-login-art,#loginView.mercato-pos-login{grid-template-columns:1fr!important}.login-hero{display:none!important}}
  `;
  document.head.appendChild(style);

  async function loadLocale() {
    try {
      const lang = window.MercatoUI?.getLanguage?.() || 'en';
      const r = await fetch(`/locales/${lang === 'fa' ? 'fa' : 'en'}.json`, { cache:'no-cache' });
      state.locale = r.ok ? await r.json() : {};
    } catch { state.locale = {}; }
  }
  const tr = text => state.locale[String(text)] || window.MercatoUI?.t?.(text) || String(text);

  async function loadPublicSettings() {
    try {
      const r = await fetch('/api/settings/public', { cache:'no-store' });
      if (r.ok) state.publicSettings = { ...state.publicSettings, ...(await r.json()) };
    } catch {}
    return state.publicSettings;
  }

  function translate(root=document.body) {
    if (!root || window.MercatoUI?.getLanguage?.() !== 'fa') return;
    const walker=document.createTreeWalker(root,NodeFilter.SHOW_TEXT),nodes=[];
    while(walker.nextNode())nodes.push(walker.currentNode);
    nodes.forEach(node=>{const p=node.parentElement,v=node.nodeValue?.trim();if(p&&!['SCRIPT','STYLE'].includes(p.tagName)&&v&&state.locale[v])node.nodeValue=node.nodeValue.replace(v,state.locale[v])});
    root.querySelectorAll?.('[placeholder],[title],[aria-label]').forEach(el=>['placeholder','title','aria-label'].forEach(a=>{const v=el.getAttribute(a);if(v&&state.locale[v])el.setAttribute(a,state.locale[v])}));
  }

  function setupLoginArtwork() {
    const view=$('loginView'); if(!view||view.dataset.artReady)return; view.dataset.artReady='1';
    if(isAdmin){view.classList.add('mercato-login-art');const hero=document.createElement('div');hero.className='login-hero';view.prepend(hero)}
    if(isPos){
      view.classList.add('mercato-pos-login');const hero=document.createElement('div');hero.className='login-hero';
      const shell=document.createElement('div');shell.className='login-form-shell';while(view.firstChild)shell.appendChild(view.firstChild);view.append(hero,shell);
    }
  }

  function setupLoginIdentity() {
    const input=isAdmin?$('loginEmail'):$('email'); if(!input)return;
    input.type=state.publicSettings.useUsername?'text':'email';input.autocomplete='username';
    const label=isAdmin?input.closest('label'):input.previousElementSibling;
    const text=state.publicSettings.useUsername?tr('Username'):tr('Email');
    if(isAdmin&&label?.firstChild?.nodeType===Node.TEXT_NODE)label.firstChild.nodeValue=text;
    else if(isPos&&label)label.textContent=text;
    input.placeholder=state.publicSettings.useUsername?tr('Username'):tr('Email');
  }

  function setupAdminRouting() {
    if(!isAdmin||typeof window.showPage!=='function'||window.showPage.__routed)return;
    const original=window.showPage;let fromHash=false;
    const wrapped=function(page){original(page);if(!fromHash)history.replaceState(null,'',`/admin/#/${encodeURIComponent(page)}`);document.title=`${document.querySelector(`#page-${CSS.escape(page)} h1`)?.textContent||'Back Office'} · Mercato`};
    wrapped.__routed=true;window.showPage=wrapped;
    const route=()=>{const page=decodeURIComponent(location.hash.match(/^#\/([^/?#]+)/)?.[1]||'dashboard');fromHash=true;try{wrapped(page)}finally{fromHash=false}};
    addEventListener('hashchange',route);
    if(typeof window.signIn==='function'){
      const originalSignIn=window.signIn;
      window.signIn=function(){try{const session=JSON.parse(localStorage.getItem('mercato.admin.session')||'null');if(!session?.user?.canAccessBackOffice){window.signOut?.();if($('loginError'))$('loginError').textContent=tr('Back Office access is not enabled for this staff member.');return}}catch{}originalSignIn();route()};
    }
  }

  function setupReferenceHelp(){if(!isAdmin)return;['goodsReference','transferReference'].forEach(id=>{const input=$(id);if(!input||input.dataset.helpAdded)return;input.dataset.helpAdded='1';const help=document.createElement('span');help.className='field-help';help.textContent=tr('Reference is an optional external document or tracking number; Mercato still creates its own internal ID.');input.closest('label')?.appendChild(help)})}

  function setupProductImageUpload(){
    if(!isAdmin)return;const url=$('productImage'),form=$('productForm');if(!url||!form||form.dataset.mediaFixed)return;form.dataset.mediaFixed='1';
    const label=url.closest('label');url.type='hidden';if(label?.firstChild?.nodeType===Node.TEXT_NODE)label.firstChild.nodeValue=tr('Product image');
    const file=document.createElement('input');file.id='productImageFile';file.className='field';file.type='file';file.accept='image/jpeg,image/png,image/webp';label?.appendChild(file);
    const preview=document.createElement('div');preview.className='media-preview';label?.appendChild(preview);const show=src=>preview.innerHTML=src?`<img src="${esc(src)}" alt=""><span class="field-help">${esc(tr('Current image'))}</span>`:'';
    file.onchange=()=>show(file.files?.[0]?URL.createObjectURL(file.files[0]):url.value);
    if(typeof window.editProduct==='function'){const old=window.editProduct;window.editProduct=id=>{old(id);file.value='';show(url.value)}}
    if(typeof window.resetProductForm==='function'){const old=window.resetProductForm;window.resetProductForm=()=>{old();file.value='';show('')}}
    form.onsubmit=async e=>{e.preventDefault();try{let imageUrl=url.value||null;if(file.files?.[0]){const session=JSON.parse(localStorage.getItem('mercato.admin.session')||'null'),fd=new FormData();fd.append('file',file.files[0]);const r=await fetch('/api/media/product-image',{method:'POST',headers:{Authorization:`Bearer ${session?.token||''}`},body:fd});const data=await r.json();if(!r.ok)throw new Error(data?.error||`Upload failed (${r.status})`);imageUrl=data.thumbnailUrl||data.imageUrl}const id=$('productId').value;await window.api('/api/products'+(id?'/'+id:''),{method:id?'PUT':'POST',body:JSON.stringify({name:$('productName').value,sku:$('productSku').value||null,imageUrl,purchasePrice:Number($('productPurchase').value),salePrice:Number($('productSale').value),categoryId:$('productCategory').value||null,artistId:$('productArtist').value||null})});window.resetProductForm?.();await window.loadProducts?.();window.toast?.(tr('Saved'))}catch(error){window.toast?.(error.message,true)}};
  }

  function setupInvoiceCustomerFilter(){if(!isAdmin||typeof window.loadInvoices!=='function'||window.loadInvoices.__fixed)return;const old=window.loadInvoices;const fixed=async function(){const select=$('invoiceCustomer');if(select){const selected=select.value,customers=await window.api('/api/customers');select.innerHTML=`<option value="">${esc(tr('All customers'))}</option>`+customers.map(x=>`<option value="${esc(x.id)}">${esc(x.name||x.phone||x.id)}</option>`).join('');select.value=selected;window.MercatoUI?.syncCombo?.(select.closest('.mcombo'))}return old()};fixed.__fixed=true;window.loadInvoices=fixed}

  function ensurePrintFooter(root=document){root.querySelectorAll?.('.printable .doc,.printable .receipt,.doc').forEach(doc=>{if(doc.querySelector('.print-powered'))return;const footer=document.createElement('div');footer.className='print-powered';footer.innerHTML='Powered by <a href="https://badje.ir">badje.ir</a>';doc.appendChild(footer)})}

  function setupSettings(){
    if(!isAdmin)return;const form=$('generalSettingsForm');if(!form||form.dataset.identityFixed)return;form.dataset.identityFixed='1';
    const target=$('showImages')?.closest('label');const label=document.createElement('label');label.style.alignContent='end';label.innerHTML=`<span><input id="useUsername" type="checkbox"> ${esc(tr('Use username for staff login'))}</span><span class="field-help">${esc(tr('When enabled, username is the login identity. When disabled, email is required for login.'))}</span>`;target?.after(label);
    if(typeof window.loadSettings==='function'){const old=window.loadSettings;window.loadSettings=async function(){const x=await old();if($('useUsername'))$('useUsername').checked=!!window.state?.settings?.useUsername||!!state.publicSettings.useUsername;return x}}
    form.onsubmit=async e=>{e.preventDefault();try{await window.api('/api/settings',{method:'PUT',body:JSON.stringify({systemLanguage:$('systemLanguage').value,posShowProductImages:$('showImages').checked,useUsername:$('useUsername').checked})});state.publicSettings.useUsername=$('useUsername').checked;state.publicSettings.systemLanguage=$('systemLanguage').value;await window.MercatoUI?.setLanguage?.($('systemLanguage').value);setupLoginIdentity();window.toast?.(tr('Saved'))}catch(error){window.toast?.(error.message,true)}};
  }

  function setupStaff(){
    if(!isAdmin)return;const form=$('staffForm');if(!form||form.dataset.identityFixed)return;form.dataset.identityFixed='1';
    const email=$('staffEmail'),emailLabel=email?.closest('label');if(!email||!emailLabel)return;
    const usernameLabel=document.createElement('label');usernameLabel.id='staffUsernameLabel';usernameLabel.className='wide';usernameLabel.innerHTML=`${esc(tr('Username'))}<input id="staffUsername" class="field" autocomplete="off">`;emailLabel.before(usernameLabel);
    const access=document.createElement('label');access.className='wide';access.innerHTML=`<span><input id="staffBackOffice" type="checkbox"> ${esc(tr('Allow Back Office access'))}</span>`;$('staffBranches')?.closest('label')?.after(access);
    const role=$('staffRole');role.innerHTML=`<option value="Cashier">${esc(tr('Cashier'))}</option><option value="Manager">${esc(tr('Manager'))}</option><option value="Admin">${esc(tr('Admin'))}</option>`;
    const applyMode=()=>{const use=!!state.publicSettings.useUsername;usernameLabel.classList.toggle('identity-hidden',!use);$('staffUsername').required=use;email.required=!use;if(emailLabel.firstChild?.nodeType===Node.TEXT_NODE)emailLabel.firstChild.nodeValue=use?tr('Email (optional)'):tr('Email')};
    applyMode();
    window.loadStaff=async function(){state.branches=await window.api('/api/branches');state.staff=await window.api('/api/staff');const bn=id=>state.branches.find(b=>b.id===id)?.name||id;const identity=x=>state.publicSettings.useUsername?(x.username||'—'):(x.email||'—');$('staffTable').innerHTML=`<div class="table-wrap"><table class="table"><thead><tr><th>${esc(state.publicSettings.useUsername?tr('Username'):tr('Email'))}</th>${state.publicSettings.useUsername?`<th>${esc(tr('Email'))}</th>`:''}<th>${esc(tr('Role'))}</th><th>${esc(tr('Back Office access'))}</th><th>${esc(tr('Assigned branches'))}</th><th>${esc(tr('Actions'))}</th></tr></thead><tbody>${state.staff.map(x=>`<tr><td>${esc(identity(x))}</td>${state.publicSettings.useUsername?`<td>${esc(x.email||'—')}</td>`:''}<td>${esc(tr(x.role))}</td><td>${x.canAccessBackOffice?'✓':'—'}</td><td>${esc((x.branchIds||[]).map(bn).join(', ')||tr('Assigned to all branches'))}</td><td><button class="btn ghost small" onclick="editStaff('${x.id}')">${esc(tr('Edit'))}</button>${x.id===window.state?.user?.id?'':` <button class="btn danger small" onclick="deleteStaff('${x.id}')">${esc(tr('Delete'))}</button>`}</td></tr>`).join('')}</tbody></table></div>`;const checks=$('staffBranches');checks.innerHTML=state.branches.map(b=>`<label class="branch-check"><input type="checkbox" value="${b.id}">${esc(b.name)}</label>`).join('');applyMode()};
    window.editStaff=id=>{const x=state.staff.find(s=>s.id===id);if(!x)return;$('staffId').value=x.id;$('staffUsername').value=x.username||'';email.value=x.email||'';role.value=x.role;$('staffPassword').value='';$('staffBackOffice').checked=!!x.canAccessBackOffice;$('staffBranches').querySelectorAll('input').forEach(i=>i.checked=(x.branchIds||[]).includes(i.value));$('staffFormTitle').textContent=tr('Edit staff member')};
    const oldReset=window.resetStaffForm;window.resetStaffForm=()=>{oldReset?.();$('staffUsername').value='';email.disabled=false;$('staffBackOffice').checked=false;applyMode()};
    form.onsubmit=async e=>{e.preventDefault();try{const id=$('staffId').value,branchIds=[...$('staffBranches').querySelectorAll('input:checked')].map(x=>x.value);const body={username:$('staffUsername').value||null,email:email.value||null,role:role.value,canAccessBackOffice:$('staffBackOffice').checked,password:$('staffPassword').value||(id?null:''),branchIds};await window.api('/api/staff'+(id?'/'+id:''),{method:id?'PUT':'POST',body:JSON.stringify(body)});window.resetStaffForm();await window.loadStaff();window.toast?.(tr('Saved'))}catch(error){window.toast?.(error.message,true)}};
  }

  async function initialize(){
    await loadPublicSettings();if(state.publicSettings.systemLanguage)await window.MercatoUI?.setLanguage?.(state.publicSettings.systemLanguage);await loadLocale();
    setupLoginArtwork();setupLoginIdentity();setupAdminRouting();setupReferenceHelp();setupProductImageUpload();setupInvoiceCustomerFilter();setupSettings();setupStaff();ensurePrintFooter();translate(document.body);
    const observer=new MutationObserver(records=>{records.forEach(r=>r.addedNodes.forEach(n=>{if(n.nodeType===1){translate(n);ensurePrintFooter(n)}}))});observer.observe(document.body,{childList:true,subtree:true});
  }
  if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',initialize);else initialize();
})();
