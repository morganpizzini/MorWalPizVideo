import { useRouteLoaderData } from "react-router-dom";
import type { DivisionOption, FieldEventInput } from "../freezerApi";

export interface RootData { session: import("../freezerApi").AccountSession; offline: boolean; }
export function rootData(): RootData { return useRouteLoaderData("root") as RootData; }
export function optionalPositiveInteger(value: FormDataEntryValue | null): number | undefined { const text=String(value ?? "").trim(); if(!text)return undefined; const number=Number(text); if(!Number.isInteger(number)||number<1) throw new Error("I dati del turno devono essere numeri interi positivi."); return number; }
export function localDateTime(value: Date): string { const pad=(part:number)=>String(part).padStart(2,"0"); return `${value.getFullYear()}-${pad(value.getMonth()+1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`; }
export function parseEventKind(value: FormDataEntryValue | null): FieldEventInput["kind"] { if(value==="turn"||value==="reconnaissance"||value==="midday"||value==="correction")return value; throw new Error("Seleziona un tipo di nota valido."); }
export function ErrorMessage({children}:{children:string}) { return <p className="notice notice-error" role="alert">{children}</p>; }
export function ContextHelp({id, children}:{id:string;children:string}) {
  return <span className="context-help">
    <button type="button" className="context-help-trigger" aria-label="Mostra spiegazione" aria-describedby={id}>?</button>
    <span id={id} className="context-help-popover" role="tooltip">{children}</span>
  </span>;
}
export function DivisionSelect({options,value,required=false}:{options:DivisionOption[];value:string|null|undefined;required?:boolean}) { return <select name="division" defaultValue={value??""} required={required}><option value="">Seleziona divisione</option>{options.map(option=><option key={option.code} value={option.code}>{option.label}</option>)}{value&&!options.some(option=>option.code===value)&&<option value={value}>Legacy non supportata: {value}</option>}</select>; }
export function errorMessage(error: unknown): string { return error instanceof Error ? error.message : "Si è verificato un errore inatteso."; }
export function safeReturnTo(value:string|null):string { if(!value||!value.startsWith("/")||value.startsWith("//"))return "/"; return value; }
export function formatDate(value:string):string { const date=new Date(value); return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("it-IT",{day:"2-digit",month:"short",year:"numeric"}).format(date); }
export function newOperationId():string { if(typeof crypto.randomUUID === "function") return crypto.randomUUID(); const bytes=crypto.getRandomValues(new Uint8Array(16)); bytes[6]=(bytes[6]&0x0f)|0x40; bytes[8]=(bytes[8]&0x3f)|0x80; const hex=Array.from(bytes,value=>value.toString(16).padStart(2,"0")).join(""); return `${hex.slice(0,8)}-${hex.slice(8,12)}-${hex.slice(12,16)}-${hex.slice(16,20)}-${hex.slice(20)}`; }
