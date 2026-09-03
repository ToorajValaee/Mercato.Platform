(() => {
  const isAdminPage = location.pathname.startsWith('/admin');
  const byId = id => document.getElementById(id);
  const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const fixState = { staff: [], branches: [], locale: {} };

  const style = document.createElement('style');
  style.textContent = `
    .lang{display:none!important}
    .panel{overflow:visible!important}
    .mcombo-menu{max-height:min(330px,calc(100vh - 24px))}
    .mcombo-options{max-height:min(250px,calc(100vh - 100px))}
    .persian-calendar{max-height:min(430px,calc(100vh - 24px));overflow:auto}
    .media-preview{display:flex;align-items:center;gap:10px;margin-top:8px}.media-preview img{width:72px;height:72px;object-fit:cover;border:1px solid #dde2e8;border-radius:10px}
    .field-help{font-size:11px;color:#65717f;font-weight:400;line-height:1.7}
  `;
  document.head.appendChild(style);

  async function loadLocale() {
    try {
      const language = window.MercatoUI?.getLanguage?.() || document.documentElement.lang || 'en';
      const response = await fetch(`/locales/${language === 'fa' ? 'fa' : 'en'}.json`, { cache: 'no-cache' });
      if (response.ok) fixState.locale = await response.json();
    } catch {}
  }
  const tr = text => fixState.locale[String(text)] || window.MercatoUI?.t?.(text) || String(text);

  function translateExternal(root = document.body) {
    if (!root || (window.MercatoUI?.getLanguage?.() !== 'fa')) return;
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    const nodes = [];
    while (walker.nextNode()) nodes.push(walker.currentNode);
    for (const node of nodes) {
      const parent = node.parentElement;
      if (!parent || ['SCRIPT','STYLE'].includes(parent.tagName)) continue;
      const value = node.nodeValue?.trim();
      if (value && fixState.locale[value]) node.nodeValue = node.nodeValue.replace(value, fixState.locale[value]);
    }
    root.querySelectorAll?.('[placeholder],[title],[aria-label]').forEach(el => {
      for (const attr of ['placeholder','title','aria-label']) {
        const value = el.getAttribute(attr);
        if (value && fixState.locale[value]) el.setAttribute(attr, fixState.locale[value]);
      }
    });
  }

  function addFavicon() {
    if (!isAdminPage || document.querySelector('link[data-mercato-favicon]')) return;
    const link = document.createElement('link');
    link.rel = 'icon'; link.type = 'image/svg+xml'; link.href = '/admin/favicon.svg'; link.dataset.mercatoFavicon = '1';
    document.head.appendChild(link);
  }

  function improvePopupPlacement() {
    document.addEventListener('click', event => {
      const button = event.target.closest?.('.mcombo-button');
      if (button) {
        const combo = button.closest('.mcombo');
        requestAnimationFrame(() => {
          const menu = combo?.querySelector('.mcombo-menu');
          if (!combo || !menu || !combo.classList.contains('open')) return;
          const rect = combo.getBoundingClientRect();
          const desired = Math.min(320, menu.scrollHeight || 320);
          const openUp = window.innerHeight - rect.bottom < desired && rect.top > desired;
          menu.style.top = openUp ? 'auto' : 'calc(100% + 5px)';
          menu.style.bottom = openUp ? 'calc(100% + 5px)' : 'auto';
        });
      }
      const dateInput = event.target.closest?.('.persian-date-input');
      if (dateInput) requestAnimationFrame(() => {
        const shell = dateInput.closest('.persian-date-shell'), calendar = shell?.querySelector('.persian-calendar');
        if (!shell || !calendar) return;
        const rect = shell.getBoundingClientRect();
        const desired = Math.min(430, calendar.scrollHeight || 430);
        const openUp = window.innerHeight - rect.bottom < desired && rect.top > desired;
        calendar.style.top = openUp ? 'auto' : '100%';
        calendar.style.bottom = openUp ? '100%' : 'auto';
      });
    }, true);
  }

  function setupRouting() {
    if (!isAdminPage || typeof window.showPage !== 'function') return;
    const originalShowPage = window.showPage;
    let fromHash = false;
    window.showPage = function(page) {
      originalShowPage(page);
      if (!fromHash) history.replaceState(null, '', `/admin/#/${encodeURIComponent(page)}`);
      document.title = `${tr(page === 'dashboard' ? 'Dashboard' : (document.querySelector(`#page-${CSS.escape(page)} h1`)?.textContent || 'Back Office'))} · Mercato`;
    };
    const routePage = () => {
      const page = decodeURIComponent((location.hash.match(/^#\/([^/?#]+)/)?.[1] || 'dashboard'));
      fromHash = true;
      try { window.showPage(page); } finally { fromHash = false; }
    };
    addEventListener('hashchange', routePage);

    if (typeof window.signIn === 'function') {
      const originalSignIn = window.signIn;
      window.signIn = function() {
        try {
          const session = JSON.parse(localStorage.getItem('mercato.admin.session') || 'null');
          if (!session?.user?.canAccessBackOffice) {
            window.signOut?.();
            const error = byId('loginError');
            if (error) error.textContent = tr('Back Office access is not enabled for this staff member.');
            return;
          }
        } catch {}
        originalSignIn();
        routePage();
      };
    }
  }

  function setupLoginLabel() {
    if (!isAdminPage) return;
    const email = byId('loginEmail');
    if (!email) return;
    email.type = 'text';
    const label = email.closest('label');
    if (label?.firstChild?.nodeType === Node.TEXT_NODE) label.firstChild.nodeValue = tr('Email / Mobile number');
  }

  function setupReferenceHelp() {
    if (!isAdminPage) return;
    ['goodsReference','transferReference'].forEach(id => {
      const input = byId(id); if (!input || input.dataset.helpAdded) return;
      input.dataset.helpAdded = '1';
      const help = document.createElement('span'); help.className = 'field-help';
      help.textContent = tr('Reference is an optional external document or tracking number; Mercato still creates its own internal ID.');
      input.closest('label')?.appendChild(help);
    });
  }

  function setupProductImageUpload() {
    if (!isAdminPage) return;
    const urlInput = byId('productImage'), form = byId('productForm');
    if (!urlInput || !form || form.dataset.mediaFixed) return;
    form.dataset.mediaFixed = '1';
    const label = urlInput.closest('label');
    urlInput.type = 'hidden';
    if (label?.firstChild?.nodeType === Node.TEXT_NODE) label.firstChild.nodeValue = tr('Product image');
    const file = document.createElement('input'); file.id = 'productImageFile'; file.className = 'field'; file.type = 'file'; file.accept = 'image/jpeg,image/png,image/webp';
    label?.appendChild(file);
    const preview = document.createElement('div'); preview.id = 'productImagePreview'; preview.className = 'media-preview'; label?.appendChild(preview);
    const updatePreview = source => { preview.innerHTML = source ? `<img src="${esc(source)}" alt=""><span class="field-help">${esc(tr('Current image'))}</span>` : ''; };
    file.addEventListener('change', () => updatePreview(file.files?.[0] ? URL.createObjectURL(file.files[0]) : urlInput.value));

    if (typeof window.editProduct === 'function') {
      const originalEdit = window.editProduct;
      window.editProduct = function(id) { originalEdit(id); file.value = ''; updatePreview(urlInput.value); };
    }
    if (typeof window.resetProductForm === 'function') {
      const originalReset = window.resetProductForm;
      window.resetProductForm = function() { originalReset(); file.value = ''; updatePreview(''); };
    }

    form.onsubmit = async event => {
      event.preventDefault();
      try {
        let imageUrl = urlInput.value || null;
        if (file.files?.[0]) {
          const session = JSON.parse(localStorage.getItem('mercato.admin.session') || 'null');
          const fd = new FormData(); fd.append('file', file.files[0]);
          const response = await fetch('/api/media/product-image', { method:'POST', headers:{ Authorization:`Bearer ${session?.token || ''}` }, body:fd });
          const data = await response.json();
          if (!response.ok) throw new Error(data?.error || `Upload failed (${response.status})`);
          imageUrl = data.thumbnailUrl || data.imageUrl;
        }
        const id = byId('productId').value;
        const body = {
          name: byId('productName').value,
          sku: byId('productSku').value || null,
          imageUrl,
          purchasePrice: Number(byId('productPurchase').value),
          salePrice: Number(byId('productSale').value),
          categoryId: byId('productCategory').value || null,
          artistId: byId('productArtist').value || null
        };
        await window.api('/api/products' + (id ? '/' + id : ''), { method:id ? 'PUT' : 'POST', body:JSON.stringify(body) });
        window.resetProductForm?.(); await window.loadProducts?.(); window.toast?.(tr('Saved'));
      } catch (error) { window.toast?.(error.message, true); }
    };
  }

  function setupInvoiceCustomerFilter() {
    if (!isAdminPage || typeof window.loadInvoices !== 'function') return;
    const original = window.loadInvoices;
    window.loadInvoices = async function() {
      const select = byId('invoiceCustomer');
      if (select) {
        const selected = select.value;
        const customers = await window.api('/api/customers');
        select.innerHTML = `<option value="">${esc(tr('All customers'))}</option>` + customers.map(x => `<option value="${esc(x.id)}">${esc(x.name || x.phone || x.id)}</option>`).join('');
        select.value = selected;
        window.MercatoUI?.syncCombo?.(select.closest('.mcombo'));
      }
      return original();
    };
  }

  function setupStaffForm() {
    if (!isAdminPage) return;
    const form = byId('staffForm'); if (!form || form.dataset.staffFixed) return;
    form.dataset.staffFixed = '1';
    const email = byId('staffEmail'); email.required = false;
    const mobileLabel = document.createElement('label');
    mobileLabel.innerHTML = `${esc(tr('Mobile number'))}<input id="staffMobile" class="field" inputmode="tel" required>`;
    email.closest('label')?.after(mobileLabel);
    const role = byId('staffRole');
    role.innerHTML = `<option value="Cashier">${esc(tr('Cashier'))}</option><option value="Manager">${esc(tr('Manager'))}</option><option value="Admin">${esc(tr('Admin'))}</option>`;
    const access = document.createElement('label'); access.className = 'wide';
    access.innerHTML = `<span><input id="staffBackOffice" type="checkbox"> ${esc(tr('Allow Back Office access'))}</span>`;
    byId('staffBranches')?.closest('label')?.after(access);

    window.loadStaff = async function() {
      fixState.branches = await window.api('/api/branches');
      fixState.staff = await window.api('/api/staff');
      const branchName = id => fixState.branches.find(b => b.id === id)?.name || id;
      const rows = fixState.staff.map(x => `<tr><td>${esc(x.mobileNumber || '—')}</td><td>${esc(x.email || '—')}</td><td>${esc(tr(x.role))}</td><td>${x.canAccessBackOffice ? '✓' : '—'}</td><td>${esc((x.branchIds || []).map(branchName).join(', ') || tr('Assigned to all branches'))}</td><td><div class="actions"><button class="btn ghost small" onclick="editStaff('${x.id}')">${esc(tr('Edit'))}</button></div></td></tr>`).join('');
      byId('staffTable').innerHTML = `<div class="table-wrap"><table class="table"><thead><tr><th>${esc(tr('Mobile number'))}</th><th>${esc(tr('Email'))}</th><th>${esc(tr('Role'))}</th><th>${esc(tr('Back Office access'))}</th><th>${esc(tr('Assigned branches'))}</th><th>${esc(tr('Actions'))}</th></tr></thead><tbody>${rows}</tbody></table></div>`;
      const checks = byId('staffBranches');
      if (checks && !checks.children.length) checks.innerHTML = fixState.branches.map(b => `<label class="branch-check"><input type="checkbox" value="${b.id}">${esc(b.name)}</label>`).join('');
    };
    window.editStaff = function(id) {
      const x = fixState.staff.find(s => s.id === id); if (!x) return;
      byId('staffId').value = x.id; email.value = x.email || ''; email.disabled = true; byId('staffMobile').value = x.mobileNumber || '';
      role.value = x.role; byId('staffPassword').value = ''; byId('staffBackOffice').checked = !!x.canAccessBackOffice;
      byId('staffBranches').querySelectorAll('input').forEach(input => input.checked = (x.branchIds || []).includes(input.value));
      byId('staffFormTitle').textContent = tr('Edit staff member');
    };
    form.onsubmit = async event => {
      event.preventDefault();
      try {
        const id = byId('staffId').value;
        const branchIds = [...byId('staffBranches').querySelectorAll('input:checked')].map(x => x.value);
        const common = { mobileNumber:byId('staffMobile').value, role:role.value, canAccessBackOffice:byId('staffBackOffice').checked, branchIds };
        const body = id ? { ...common, password:byId('staffPassword').value || null } : { ...common, email:email.value || null, password:byId('staffPassword').value };
        await window.api('/api/staff' + (id ? '/' + id : ''), { method:id ? 'PUT' : 'POST', body:JSON.stringify(body) });
        window.resetStaffForm?.(); byId('staffMobile').value = ''; byId('staffBackOffice').checked = false; await window.loadStaff(); window.toast?.(tr('Saved'));
      } catch (error) { window.toast?.(error.message, true); }
    };
  }

  async function initialize() {
    await loadLocale();
    addFavicon(); improvePopupPlacement(); setupRouting(); setupLoginLabel(); setupReferenceHelp(); setupProductImageUpload(); setupInvoiceCustomerFilter(); setupStaffForm();
    translateExternal(document.body);
    const observer = new MutationObserver(records => records.forEach(record => record.addedNodes.forEach(node => { if (node.nodeType === 1) translateExternal(node); })));
    observer.observe(document.body, { childList:true, subtree:true });
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize); else initialize();
})();
