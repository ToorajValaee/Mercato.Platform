import React, { useEffect, useMemo, useRef, useState } from 'react';

export type User = { id:string; email?:string|null; username?:string|null; role:string; canAccessBackOffice:boolean };
export type PublicSettings = { systemLanguage:'en'|'fa'; posShowProductImages:boolean; useUsername:boolean; paymentMethods:any[]; discounts:any[] };

const sessionKey = 'mercato.frontend.session';
export function saveSession(token:string,user:User){localStorage.setItem(sessionKey,JSON.stringify({token,user}))}
export function loadSession():{token:string;user:User}|null{try{return JSON.parse(localStorage.getItem(sessionKey)||'null')}catch{return null}}
export function clearSession(){localStorage.removeItem(sessionKey)}

export async function api<T=any>(path:string, options:RequestInit={}, token?:string|null):Promise<T>{
  const headers = new Headers(options.headers||{});
  if(token) headers.set('Authorization',`Bearer ${token}`);
  if(options.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type','application/json');
  const response = await fetch(path,{...options,headers});
  const text = await response.text();
  let data:any = null;
  if(text){try{data=JSON.parse(text)}catch{data=text}}
  if(!response.ok) throw new Error(data?.error||data?.title||(typeof data==='string'?data:`Request failed (${response.status})`));
  return data as T;
}
export const json = (value:any)=>JSON.stringify(value);

let locale:Record<string,string>={}; let language:'en'|'fa'='en';
export async function setLanguage(value:'en'|'fa'){
  language=value; document.documentElement.lang=value; document.documentElement.dir=value==='fa'?'rtl':'ltr';
  try{locale=await fetch(`/locales/${value}.json`,{cache:'no-cache'}).then(r=>r.ok?r.json():{})}catch{locale={}}
}
export function t(value:string){return locale[value]||value}
export function getLanguage(){return language}
export function money(value:any){return Number(value||0).toLocaleString(language==='fa'?'fa-IR':'en-US',{minimumFractionDigits:2,maximumFractionDigits:2})}
export function fmtDate(value:any){if(!value)return '—';const d=new Date(value);return new Intl.DateTimeFormat(language==='fa'?'fa-IR-u-ca-persian':'en-US',{year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit'}).format(d)}

export function usePublicSettings(){const [settings,setSettings]=useState<PublicSettings|null>(null);useEffect(()=>{api<PublicSettings>('/api/settings/public').then(async s=>{await setLanguage(s.systemLanguage||'en');setSettings(s)}).catch(()=>setSettings({systemLanguage:'en',posShowProductImages:false,useUsername:false,paymentMethods:[],discounts:[]}))},[]);return settings}
export function useToast(){const [toast,setToast]=useState<{text:string,error?:boolean}|null>(null);function show(text:string,error=false){setToast({text,error});window.setTimeout(()=>setToast(null),3500)}return{toast,show}}
export function Toast({toast}:{toast:{text:string,error?:boolean}|null}){return toast?<div className={'toast'+(toast.error?' error':'')}>{toast.text}</div>:null}

export function Button({children,className='',...props}:React.ButtonHTMLAttributes<HTMLButtonElement>){return <button className={`btn ${className}`} {...props}>{children}</button>}
export function Panel({title,actions,children,className=''}:{title?:React.ReactNode;actions?:React.ReactNode;children:React.ReactNode;className?:string}){return <section className={`panel ${className}`}>{title||actions?<div className="panel-head"><h2>{title}</h2><div className="row">{actions}</div></div>:null}<div className="panel-body">{children}</div></section>}
export function PageHeader({title,subtitle,actions}:{title:string;subtitle?:string;actions?:React.ReactNode}){return <div className="page-head"><div><h1>{t(title)}</h1>{subtitle?<p>{t(subtitle)}</p>:null}</div>{actions?<div className="row">{actions}</div>:null}</div>}
export function Field({label,children,className=''}:{label:string;children:React.ReactNode;className?:string}){return <label className={className}>{t(label)}{children}</label>}

export function SearchSelect({value,onChange,options,placeholder='Select…',disabled=false}:{value:string;onChange:(v:string)=>void;options:{value:string;label:string}[];placeholder?:string;disabled?:boolean}){
  const [open,setOpen]=useState(false),[q,setQ]=useState(''); const button=useRef<HTMLButtonElement>(null); const [rect,setRect]=useState<DOMRect|null>(null);
  const current=options.find(x=>x.value===value); const filtered=options.filter(x=>x.label.toLowerCase().includes(q.toLowerCase()));
  useEffect(()=>{const close=(e:MouseEvent)=>{if(!(e.target as HTMLElement).closest('.search-select') && !(e.target as HTMLElement).closest('.search-select-menu'))setOpen(false)};document.addEventListener('mousedown',close);return()=>document.removeEventListener('mousedown',close)},[]);
  const toggle=()=>{if(disabled)return;const r=button.current?.getBoundingClientRect()||null;setRect(r);setOpen(x=>!x);setQ('')};
  const style:React.CSSProperties|undefined=rect?{left:document.dir==='rtl'?undefined:rect.left,right:document.dir==='rtl'?window.innerWidth-rect.right:undefined,width:Math.max(rect.width,240),top:Math.min(rect.bottom+5,window.innerHeight-390)}:undefined;
  return <div className="search-select"><button ref={button} type="button" className="search-select-button" onClick={toggle} disabled={disabled}>{current?.label||t(placeholder)}</button>{open&&rect?<div className="search-select-menu" style={style}><input autoFocus value={q} onChange={e=>setQ(e.target.value)} placeholder={t('Search…')}/><div className="search-options">{filtered.length?filtered.map(o=><button type="button" className={'search-option'+(o.value===value?' active':'')} key={o.value} onClick={()=>{onChange(o.value);setOpen(false)}}>{o.label}</button>):<div className="empty">{t('No records found.')}</div>}</div></div>:null}</div>
}

export function PaginatedTable({rows,columns,actions,pageSize=10}:{rows:any[];columns:{label:string;render:(r:any)=>React.ReactNode}[];actions?:(r:any)=>React.ReactNode;pageSize?:number}){
  const [page,setPage]=useState(1);useEffect(()=>setPage(1),[rows.length]);const pages=Math.max(1,Math.ceil(rows.length/pageSize));const slice=rows.slice((page-1)*pageSize,page*pageSize);
  return <><div className="table-wrap responsive-table"><table className="table"><thead><tr>{columns.map(c=><th key={c.label}>{t(c.label)}</th>)}{actions?<th>{t('Actions')}</th>:null}</tr></thead><tbody>{slice.map((r,i)=><tr key={r.id||i}>{columns.map(c=><td key={c.label}>{c.render(r)}</td>)}{actions?<td><div className="table-actions">{actions(r)}</div></td>:null}</tr>)}</tbody></table><div className="mobile-card-list">{slice.map((r,i)=><div className="mobile-record" key={r.id||i}>{columns.map(c=><div className="mobile-record-row" key={c.label}><span className="k">{t(c.label)}</span><span className="v">{c.render(r)}</span></div>)}{actions?<div className="row">{actions(r)}</div>:null}</div>)}</div></div>{!rows.length?<div className="empty">{t('No records found.')}</div>:null}<div className="pager"><span>{t('Page')} {page} {t('of')} {pages} · {rows.length} {t('rows')}</span><div className="row"><Button className="ghost small" disabled={page<=1} onClick={()=>setPage(p=>p-1)}>{t('Previous')}</Button><Button className="ghost small" disabled={page>=pages} onClick={()=>setPage(p=>p+1)}>{t('Next')}</Button></div></div></>
}

export function Dialog({title,onClose,children,printable=false}:{title:string;onClose:()=>void;children:React.ReactNode;printable?:boolean}){return <div className="dialog-backdrop" onMouseDown={e=>{if(e.target===e.currentTarget)onClose()}}><div className={'dialog'+(printable?' printable':'')}><div className="dialog-head no-print"><strong>{t(title)}</strong><Button className="ghost small" onClick={onClose}>×</Button></div><div className="dialog-body">{children}</div></div></div>}

export function DocumentView({title,data}:{title:string;data:any}){const items=data.items||[];return <div className="doc"><div className="doc-head"><div><h2 style={{margin:0}}>Mercato {t(title)}</h2><div className="muted">{data.id||data.orderId}</div></div><div style={{textAlign:'end'}}><strong>{fmtDate(data.createdAtUtc||data.createdAt||data.paidAtUtc)}</strong><div>{data.branchName||''}</div></div></div>{data.customerName?<p><strong>{t('Customer')}:</strong> {data.customerName}{data.customerPhone?` · ${data.customerPhone}`:''}</p>:null}<table><thead><tr><th>{t('Product')}</th><th>{t('Quantity')}</th><th>{t('Sale price')}</th><th>{t('Total')}</th></tr></thead><tbody>{items.map((i:any)=><tr key={i.id||i.productId}><td>{i.productName||i.productId}</td><td>{i.quantity??i.soldQuantity}</td><td>{money(i.unitPrice)}</td><td>{money(i.lineTotal)}</td></tr>)}</tbody></table><div className="doc-totals"><div><span>{t('Subtotal')}</span><span>{money(data.subtotalAmount??data.subtotal??data.totalAmount)}</span></div>{Number(data.discountAmount)>0?<div><span>{t('Discount')} {data.discountName||''}</span><span>−{money(data.discountAmount)}</span></div>:null}<div className="grand"><span>{t('Total')}</span><span>{money(data.totalAmount??data.total)}</span></div></div><div className="print-footer">Powered by badje.ir</div></div>}

export function Login({title,hero,settings,onSuccess,requireBackOffice=false}:{title:string;hero:string;settings:PublicSettings;onSuccess:(token:string,user:User)=>void;requireBackOffice?:boolean}){
  const [identifier,setIdentifier]=useState(''),[password,setPassword]=useState(''),[error,setError]=useState(''),[busy,setBusy]=useState(false);const label=settings.useUsername?'Username':'Email';
  async function submit(e:React.FormEvent){e.preventDefault();setBusy(true);setError('');try{const result=await api<any>('/api/auth/login',{method:'POST',body:json({identifier,password})});if(requireBackOffice&&!result.user?.canAccessBackOffice)throw new Error(t('Back Office access is not enabled for this staff member.'));saveSession(result.token,result.user);onSuccess(result.token,result.user)}catch(err:any){setError(err.message||'Login failed')}finally{setBusy(false)}}
  return <div className="login-page"><div className="login-form-wrap"><form className="login-card stack" onSubmit={submit}><div className="brand" style={{color:'var(--ink)',padding:0}}><span className="brand-mark">M</span> Mercato</div><div><h1 style={{marginBottom:6}}>{t(title)}</h1><div className="muted">{settings.useUsername?t('Sign in with your username.'):t('Sign in with your email address.')}</div></div><Field label={label}><input className="input" autoComplete="username" value={identifier} onChange={e=>setIdentifier(e.target.value)} required/></Field><Field label="Password"><input className="input" type="password" autoComplete="current-password" value={password} onChange={e=>setPassword(e.target.value)} required/></Field>{error?<div style={{color:'var(--danger)',fontSize:13}}>{error}</div>:null}<Button disabled={busy}>{busy?t('Loading…'):t('Sign in')}</Button></form></div><div className="login-hero"><img src={hero} alt=""/></div></div>
}

export function useRoute(base:string, defaultPage:string){const pageFromPath=()=>{const p=location.pathname.replace(new RegExp(`^${base}/?`),'').split('/')[0];return p||defaultPage};const [page,setPage]=useState(pageFromPath);useEffect(()=>{const h=()=>setPage(pageFromPath());addEventListener('popstate',h);return()=>removeEventListener('popstate',h)},[]);function navigate(next:string){history.pushState({},'',`${base}/${next}`);setPage(next)}return{page,navigate}}

export function Chart({values}:{values:{label:string;value:number}[]}){const max=Math.max(1,...values.map(x=>x.value));return <div className="chart">{values.map((x,i)=><div className="chart-col" key={i}><strong style={{fontSize:10}}>{Math.round(x.value)}</strong><div className="chart-bar" style={{height:`${Math.max(2,x.value/max*150)}px`}}/><span className="chart-label">{x.label}</span></div>)}</div>}
