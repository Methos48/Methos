from __future__ import annotations
import os, re, json, struct, xml.etree.ElementTree as ET
from pathlib import Path

# Interlude (ct0) schema: field order follows the public ct0 XDAT schema.
BASE = [
 ('name','string'),('superName','string'),('unk2','int'),('unk3','int'),('unk4','string'),('unk5','string'),('unk6','string'),('unk7','int'),
 ('size','bool'),('IF','size'),('size__absolute__values','bool'),('IFNOT','size__absolute__values'),('_reserved_0','compact'),('size__percent__width','float'),('size__percent__height','float'),('END',''),('size__absolute__width','int'),('size__absolute__height','int'),('END',''),
 ('anchor','bool'),('IF','anchor'),('anchor__parent','enum:alignment'),('anchor__this','enum:alignment'),('anchor__ctrl','string'),('anchor__x','int'),('anchor__y','int'),('END',''),('unk22','int'),('unk23','int'),('unk24','int'),('popupType','string'),('popupValue','int')]

SCHEMA = {
'ct0.DefaultProperty': (None, BASE),
'ct0.Window': ('ct0.DefaultProperty', [('unk100','string'),('unk101','string'),('unk102','string'),('unk103','string'),('unk104','int'),('unk105','int'),('unk106','int'),('unk107','int'),('unk108','int'),('unk109','int'),('unk110','int'),('unk111','int'),('unk112','int'),('unk113','int'),('unk114','int'),('unk115','int'),('unk116','int'),('unk117','int'),('unk118','int'),('unk119','int'),('unk120','float'),('unk121','float'),('unk122','int'),('unk123','int'),('unk124','int'),('unk125','int'),('unk126','int'),('unk127','int'),('unk128','int'),('unk129','string'),('unk130','int'),('unk131','int'),('unk132','int'),('unk133','int'),('unk134','int'),('unk135','int'),('unk136','string'),('unk137','int'),('unk138','int'),('unk139','string'),('children','list:poly')]),
'ct0.BarCtrl': ('ct0.DefaultProperty',[('foreTexture','string'),('backTexture','string'),('uSize','int'),('vSize','int')]),
'ct0.Button': ('ct0.DefaultProperty',[('normalTex','string'),('pushedTex','string'),('highlightTex','string'),('dropTex','string'),('buttonName','int'),('noHighlight','int'),('defaultSoundOn','int'),('disableTime','int')]),
'ct0.ChatWindow': ('ct0.DefaultProperty',[('lineGap','int')]),
'ct0.CheckBox': ('ct0.DefaultProperty',[('titleIndex','int'),('checked','bool'),('leftAligned','bool'),('maxWidth','int'),('checkTexture','string'),('unCheckTexture','string'),('disableTexture','string'),('disableCheckTexture','string')]),
'ct0.ComboBox': ('ct0.DefaultProperty',[('values','list:ct0.ComboBox.ComboBoxElement')]),
'ct0.ComboBox.ComboBoxElement': (None,[('sysString','int'),('systemMsg','int'),('text','string'),('reserved','int')]),
'ct0.EditBox': ('ct0.DefaultProperty',[('type','enum:editbox'),('maxLength','int'),('showCursor','int'),('chatMarkOn','bool')]),
'ct0.EffectButton': ('ct0.DefaultProperty',[('type','int'),('normalTex','string'),('pushedTex','string'),('highlightTex','string'),('effectTex1','string'),('effectTex2','string')]),
'ct0.FishViewportWindow': ('ct0.DefaultProperty',[('texBack','string'),('texClock','string'),('texFishHPBar','string'),('texFishHPBarBack','string'),('texFishFakeHPBarWarning','string'),('texFishingEffect','string'),('texIconPumping','string'),('texIconReeling','string')]),
'ct0.HtmlCtrl': ('ct0.DefaultProperty',[('viewType','string')]),
'ct0.InvenWeight': ('ct0.DefaultProperty',[]),
'ct0.ItemWindow': ('ct0.DefaultProperty',[('col','int'),('row','int'),('maxItemNum','int'),('iconWidth','int'),('iconHeight','int'),('gapX','int'),('gapY','int'),('offsetX','int'),('offsetY','int'),('backgroundItemWidth','int'),('backgroundItemHeight','int'),('backgroundItemTex','string'),('selectedItemWidth','int'),('selectedItemHeight','int'),('selectedItemTex','string'),('unselectedItemWidth','int'),('unselectedItemHeight','int'),('unselectedItemTex','string'),('noSelectItem','int'),('noItemDrag','int'),('buttonClick','int'),('useCoolTime','int'),('noScroll','int'),('outLineUp','string'),('outLineDown','string')]),
'ct0.ListCtrl': ('ct0.DefaultProperty',[('unk100','int'),('unk101','int'),('unk102','int'),('unk103','int'),('unk104','int'),('values','list:ct0.ListCtrl.ListElement')]),
'ct0.ListCtrl.ListElement': (None,[('textStringId','int'),('width','int'),('unk108','bool'),('unk109','bool'),('unk110','bool')]),
'ct0.MinimapCtrl': ('ct0.DefaultProperty',[('showTime','bool'),('showTown','bool'),('showGrid','bool'),('showMyLocMark','bool'),('showMyLocText','bool'),('showSSQText','bool')]),
'ct0.MultiEdit': ('ct0.DefaultProperty',[('unk100','int'),('unk101','int')]),
'ct0.MultiSellItemInfo': ('ct0.DefaultProperty',[]),
'ct0.MultiSellNeededItem': ('ct0.DefaultProperty',[]),
'ct0.NameCtrl': ('ct0.DefaultProperty',[]),
'ct0.Progress': ('ct0.DefaultProperty',[('backTexture','string'),('barTexture','string'),('gap','int')]),
'ct0.Radar': ('ct0.DefaultProperty',[]),
'ct0.RadioButton': ('ct0.DefaultProperty',[('sysstring','int'),('radioGroupID','int'),('isChecked','int')]),
'ct0.ScrollArea': ('ct0.DefaultProperty',[('areaHeight','int'),('children','list:poly')]),
'ct0.Shortcut': (None,[('unk0','string'),('unk1','string'),('actions','list:ct0.Shortcut.Action')]),
'ct0.Shortcut.Action': (None,[('key_1','enum:key'),('key_2','enum:key'),('key_3','enum:key'),('action','string')]),
'ct0.ShortcutItemWindow': ('ct0.DefaultProperty',[('alwaysShowOutline','bool')]),
'ct0.SliderCtrl': ('ct0.DefaultProperty',[('numOfTick','int'),('currTick','int'),('thumbBtnWidth','int'),('thumbBtnHeight','int'),('backTexture','string'),('disableBackTexture','string'),('thumbBtnNormalTexture','string'),('thumbBtnDownTexture','string'),('pushBtnWidth','int'),('pushBtnHeight','int'),('pushBtnAutoHitTime','int'),('unk111','int'),('tickTexture','string')]),
'ct0.StatusBar': ('ct0.DefaultProperty',[('title','string'),('textureWidth','int'),('textureHeight','int'),('foreTexture','string'),('backTexture','string'),('warnTexture','string'),('regenTexture','string')]),
'ct0.StatusIconCtrl': ('ct0.DefaultProperty',[('noClip','bool'),('noTooltip','bool')]),
'ct0.Tab': ('ct0.DefaultProperty',[('tabs','list:ct0.Tab.TabElement')]),
'ct0.Tab.TabElement': (None,[('buttonName','int'),('target','string'),('width','int'),('height','int'),('normalTex','string'),('pushedTex','string'),('movable','bool'),('gap','int'),('tooltip','int'),('noHighlight','int')]),
'ct0.TextBox': ('ct0.DefaultProperty',[('text','string'),('textAlign','enum:textalign'),('offsetY','int'),('backTex','string'),('fontType','int'),('sysstring','int'),('systemMsg','int'),('textColor','color'),('emoticon','int'),('autosize','int')]),
'ct0.TextListBox': ('ct0.DefaultProperty',[('maxRow','int'),('showRow','int'),('lineGap','int'),('isShowScroll','int')]),
'ct0.Texture': ('ct0.DefaultProperty',[('file','string'),('type','enum:texturetype'),('layer','enum:texturelayer'),('u','float'),('v','float'),('uSize','float'),('vSize','float'),('alpha','int'),('isAnimTex','int')]),
'ct0.TreeCtrl': ('ct0.DefaultProperty',[('saveExpandedNode','bool'),('multiExpand','int')]),
'ct0.WndDefPos': (None,[('wnd','string'),('alignment','enum:alignment'),('x','int'),('y','int'),('moveParent','bool'),('width','int'),('height','int')]),
'ct0.XDAT': (None,[('windows','list:ct0.Window'),('shortcuts','list:ct0.Shortcut'),('_reserved_0','int'),('wndDefPos','list:ct0.WndDefPos'),('_tail','tail')]),
}

# Inner Java class names used by the supplied XML export.
XML_TO_TYPE={
 'ct0.Shortcut_-Action':'ct0.Shortcut.Action',
 'ct0.ListCtrl_-ListElement':'ct0.ListCtrl.ListElement',
 'ct0.ComboBox_-ComboBoxElement':'ct0.ComboBox.ComboBoxElement',
 'ct0.Tab_-TabElement':'ct0.Tab.TabElement',
}
TYPE_TO_XML={v:k for k,v in XML_TO_TYPE.items()}
ENUMS={
 'alignment':{'NONE':0,'TOP_LEFT':1,'TOP_CENTER':2,'TOP_RIGHT':3,'CENTER_LEFT':4,'CENTER':5,'CENTER_RIGHT':6,'BOTTOM_LEFT':7,'BOTTOM_CENTER':8,'BOTTOM_RIGHT':9},
 'editbox':{'NORMAL':0,'CHAT':1,'PASSWORD':2,'COUNT':3,'NUMBER':4,'UNK5':5,'TYPE6':6},
 'textalign':{'Undefined':0,'Left':1,'Center':2,'Right':3,'MacroIcon':4},
 'texturetype':{'Stretch':0,'Normal':1,'Tile':2,'Draggable':3,'Control':4,'Mask':5},
 'texturelayer':{'None':0,'Normal':1,'Background':2},
}
KEYS={'IK_None':0,'IK_LeftMouse':1,'IK_RightMouse':2,'IK_Cancel':3,'IK_MiddleMouse':4,'IK_Backspace':8,'IK_Tab':9,'IK_Enter':13,'IK_Shift':16,'IK_Ctrl':17,'IK_Alt':18,'IK_Pause':19,'IK_CapsLock':20,'IK_Escape':27,'IK_Space':32,'IK_PageUp':33,'IK_PageDown':34,'IK_End':35,'IK_Home':36,'IK_Left':37,'IK_Up':38,'IK_Right':39,'IK_Down':40,'IK_Insert':45,'IK_Delete':46,'IK_NumLock':144,'IK_ScrollLock':145,'IK_LShift':160,'IK_RShift':161,'IK_LControl':162,'IK_RControl':163,'IK_MouseWheelUp':236,'IK_MouseWheelDown':237}
for i in range(10): KEYS[f'IK_{i}']=48+i; KEYS[f'IK_NumPad{i}']=96+i
for i,c in enumerate('ABCDEFGHIJKLMNOPQRSTUVWXYZ'): KEYS[f'IK_{c}']=65+i
for i in range(1,25): KEYS[f'IK_F{i}']=111+i
for label,val in [('IK_GreyStar',106),('IK_GreyPlus',107),('IK_Separator',108),('IK_GreyMinus',109),('IK_NumPadPeriod',110),('IK_GreySlash',111),('IK_Semicolon',186),('IK_Equals',187),('IK_Comma',188),('IK_Minus',189),('IK_Period',190),('IK_Slash',191),('IK_Tilde',192),('IK_LeftBracket',219),('IK_Backslash',220),('IK_RightBracket',221),('IK_SingleQuote',222)]: KEYS[label]=val
KEYS_REV={v:k for k,v in KEYS.items()}

class Reader:
 def __init__(self,b): self.b=b; self.p=0
 def take(self,n):
  if n<0 or self.p+n>len(self.b): raise ValueError(f'Archivo incompleto en 0x{self.p:X}')
  x=self.b[self.p:self.p+n]; self.p+=n; return x
 def u8(self): return self.take(1)[0]
 def i32(self): return struct.unpack('<i',self.take(4))[0]
 def u32(self): return struct.unpack('<I',self.take(4))[0]
 def f32(self): return struct.unpack('<f',self.take(4))[0]
 def compact(self):
  first=self.u8(); value=first&63; shift=6; more=bool(first&64)
  for i in range(1,5):
   if not more: break
   c=self.u8(); value |= (c & (31 if i==4 else 127)) << shift; shift+=7; more=(i<4 and bool(c&128))
  if value>0x7fffffff: raise ValueError('Entero compacto fuera de rango')
  return -value if first&128 else value
 def string(self):
  n=self.compact(); size=abs(n)*(2 if n<0 else 1)
  if size>16*1024*1024: raise ValueError('Cadena demasiado grande')
  raw=self.take(size)
  if not size:return ''
  if n>0:
   if raw[-1:]!=b'\0': raise ValueError('Cadena ANSI sin terminador')
   return raw[:-1].decode('latin-1')
  if size<2 or raw[-2:]!=b'\0\0': raise ValueError('Cadena UTF-16 sin terminador')
  return raw[:-2].decode('utf-16le')

def put_compact(v):
 n=abs(int(v)); first=(n&63)|(0x80 if v<0 else 0); n>>=6; out=bytearray([first|(0x40 if n else 0)])
 while n:
  c=n&127; n>>=7; out.append(c|(0x80 if n else 0))
 return bytes(out)
def put_string(s):
 s=str(s)
 if not s:return put_compact(0)
 if all(ord(c)<=0xFF for c in s):
  # UE1/UE2 ANSI FString values are Latin-1 bytes, not UTF-16 merely because
  # a character is outside ASCII. Preserve the original byte representation.
  b=s.encode('latin-1')+b'\0'; return put_compact(len(b))+b
 b=s.encode('utf-16le')+b'\0\0'; return put_compact(-(len(b)//2))+b

def ops_for(t):
 base, own=SCHEMA[t]
 return (ops_for(base) if base else [])+own

def truth(v): return v==1 or v is True

def skip_block(ops, index):
 level=1
 while index < len(ops) and level:
  token=ops[index][0]
  if token in ('IF','IFNOT'): level+=1
  elif token=='END': level-=1
  index+=1
 return index

def parse_node(t,r,depth=0):
 if depth>100: raise ValueError('Estructura demasiado profunda')
 if t not in SCHEMA: raise ValueError('Tipo XDAT desconocido: '+t)
 obj={'__type__':t,'__raw__':{},'__tag__':b''}
 ops=ops_for(t); i=0
 while i<len(ops):
  name,kind=ops[i]; i+=1
  if name=='IF':
   if not truth(obj.get(kind,0)):
    i=skip_block(ops,i)
   continue
  if name=='IFNOT':
   if truth(obj.get(kind,0)):
    i=skip_block(ops,i)
   continue
  if name=='END': continue
  if kind=='tail': obj[name]=r.take(len(r.b)-r.p); continue
  start=r.p
  if kind.startswith('list:'):
   count=r.i32();
   if count<0 or count>100000: raise ValueError(f'Cantidad de elementos inválida en {t}.{name}: {count}')
   itemtype=kind[5:]; items=[]
   for _ in range(count):
    if itemtype=='poly':
     tagstart=r.p; short=r.string(); tagraw=r.b[tagstart:r.p]; childtype='ct0.'+short
     if childtype not in SCHEMA: raise ValueError('Tipo de control no soportado: '+childtype)
     child=parse_node(childtype,r,depth+1); child['__tag__']=tagraw
    else: child=parse_node(itemtype,r,depth+1)
    items.append(child)
   obj[name]=items; obj['__raw__'][name]=r.b[start:r.p]
  elif kind=='string': obj[name]=r.string(); obj['__raw__'][name]=r.b[start:r.p]
  elif kind=='int': obj[name]=r.i32(); obj['__raw__'][name]=r.b[start:r.p]
  elif kind=='bool': obj[name]=r.i32(); obj['__raw__'][name]=r.b[start:r.p]
  elif kind=='float': obj[name]=r.f32(); obj['__raw__'][name]=r.b[start:r.p]
  elif kind=='compact': obj[name]=r.compact(); obj['__raw__'][name]=r.b[start:r.p]
  elif kind=='color': obj[name]=r.u32(); obj['__raw__'][name]=r.b[start:r.p]
  elif kind.startswith('enum:'): obj[name]=r.i32(); obj['__raw__'][name]=r.b[start:r.p]
  else: raise ValueError('Tipo de campo no soportado: '+kind)
 return obj

def write_node(obj,out,depth=0):
 if depth>100: raise ValueError('Estructura demasiado profunda')
 t=obj['__type__']; ops=ops_for(t); i=0
 while i<len(ops):
  name,kind=ops[i]; i+=1
  if name in ('IF','IFNOT'):
   v=truth(obj.get(kind,0)); skip=(not v if name=='IF' else v)
   if skip: i=skip_block(ops,i)
   continue
  if name=='END': continue
  if kind=='tail': out.extend(obj.get(name,b'')); continue
  if kind.startswith('list:'):
   if name in obj.get('__raw__',{}):
    out.extend(obj['__raw__'][name]); continue
   items=obj.get(name,[]); out.extend(struct.pack('<i',len(items)))
   itemtype=kind[5:]
   for child in items:
    if itemtype=='poly':
     tag=child.get('__tag__',b'')
     if tag: out.extend(tag)
     else: out.extend(put_string(child['__type__'].split('.',1)[1]))
    write_node(child,out,depth+1)
   continue
  raw=obj.get('__raw__',{}).get(name)
  if raw is not None:
   out.extend(raw); continue
  v=obj.get(name,0)
  if kind=='string': out.extend(put_string(v))
  elif kind in ('int','bool') or kind.startswith('enum:'): out.extend(struct.pack('<i',int(v)))
  elif kind=='float': out.extend(struct.pack('<f',float(v)))
  elif kind=='compact': out.extend(put_compact(int(v)))
  elif kind=='color': out.extend(struct.pack('<I',int(v)&0xffffffff))
  else: raise ValueError(f'No se puede escribir {t}.{name} ({kind})')

def parse_xdat(data):
 r=Reader(data); root=parse_node('ct0.XDAT',r)
 if r.p!=len(data): raise ValueError(f'Quedaron {len(data)-r.p} bytes sin interpretar en 0x{r.p:X}; el archivo puede no ser Interlude ct0')
 return root

def enum_label(kind,value):
 if kind=='enum:key': return KEYS_REV.get(value,f'IK_Unknown{value:02X}')
 table=ENUMS.get(kind.split(':',1)[1],{})
 return next((k for k,v in table.items() if v==value),str(value))

def xml_tag(t): return TYPE_TO_XML.get(t,t)
def xml_to_type(tag): return XML_TO_TYPE.get(tag,tag)

def xml_value(el,kind):
 s=el.text or ''
 if kind=='string': return s
 if kind in ('int','compact'): return int(s or 0)
 if kind=='bool':
  if s.lower() in ('true','1'): return 1
  if s.lower() in ('false','0',''): return 0
  return int(s)
 if kind=='float': return float(s or 0)
 if kind.startswith('enum:'):
  name=kind.split(':',1)[1]; val=s.strip()
  if name=='key':
   if val in KEYS:return KEYS[val]
   if val.startswith('IK_Unknown'):
    try:return int(val[10:],16)
    except:pass
   raise ValueError('Tecla no reconocida: '+val)
  if val in ENUMS.get(name,{}): return ENUMS[name][val]
  return int(val)
 if kind=='color':
  ch={c:float(el.findtext(c,'0')) for c in ('red','green','blue','opacity')}
  # Lineage color word uses RGBA byte order.
  rgba=[max(0,min(255,round(ch[c]*255))) for c in ('red','green','blue','opacity')]
  return (rgba[3]<<24)|(rgba[0]<<16)|(rgba[1]<<8)|rgba[2]
 raise ValueError('Tipo XML no soportado: '+kind)

def parse_xml_object(el,t=None):
 t=t or xml_to_type(el.tag)
 if t not in SCHEMA: raise ValueError('Tipo XML no soportado: '+el.tag)
 obj={'__type__':t,'__raw__':{},'__tag__':b''}
 children={c.tag:c for c in list(el) if c.tag!='outer-class'}
 for name,kind in ops_for(t):
  if name in ('IF','IFNOT','END'): continue
  if kind=='tail': obj[name]=b''; continue
  field=children.get(name)
  if field is None:
   # The XML export can omit inactive conditional fields; active fields must exist.
   continue
  if kind.startswith('list:'):
   backing=field.find('backingList')
   candidates=list(backing) if backing is not None else list(field)
   itemtype=kind[5:]; arr=[]
   for child_el in candidates:
    if child_el.tag in ('changeBuilder','changeLock','list','backingList'): continue
    ct=xml_to_type(child_el.tag) if itemtype=='poly' else itemtype
    child=parse_xml_object(child_el,ct)
    if itemtype=='poly': child['__tag__']=put_string(ct.split('.',1)[1])
    arr.append(child)
   obj[name]=arr
  elif kind=='color': obj[name]=xml_value(field,kind)
  else: obj[name]=xml_value(field,kind)
 # Map XML's double underscore fields for inner classes.
 if t=='ct0.Shortcut.Action':
  for k in ('key_1','key_2','key_3'):
   if k not in obj and k.replace('_','__') in children: obj[k]=xml_value(children[k.replace('_','__')],'enum:key')
 return obj

def xml_scalar(parent,name,kind,value):
 el=ET.SubElement(parent,name)
 if kind=='bool': el.text='-1' if int(value)==-1 else ('true' if truth(value) else 'false')
 elif kind.startswith('enum:'): el.text=enum_label(kind,value)
 elif kind=='color':
  rgba=[(int(value)>>16)&255,(int(value)>>8)&255,int(value)&255,(int(value)>>24)&255]
  for c,v in zip(('red','green','blue','opacity'),rgba): ET.SubElement(el,c).text=str(v/255.0)
 else: el.text=str(value)
 return el

def active_fields(obj):
 ops=ops_for(obj['__type__']); i=0; result=[]
 while i<len(ops):
  name,kind=ops[i]; i+=1
  if name=='IF':
   if not truth(obj.get(kind,0)): i=skip_block(ops,i)
   continue
  if name=='IFNOT':
   if truth(obj.get(kind,0)): i=skip_block(ops,i)
   continue
  if name=='END': continue
  result.append((name,kind))
 return result

def object_to_xml(obj):
 t=obj['__type__']; root=ET.Element(xml_tag(t))
 for name,kind in active_fields(obj):
  if name in ('_reserved_0','_tail'): continue
  xmlname=name
  if t=='ct0.Shortcut.Action' and name.startswith('key_'): xmlname=name.replace('_','__',1)
  value=obj.get(name,[] if kind.startswith('list:') else '')
  if kind.startswith('list:'):
   wrap=ET.SubElement(root,xmlname,{'class':'com.sun.javafx.collections.ObservableListWrapper'})
   change=ET.SubElement(wrap,'changeBuilder'); ET.SubElement(change,'list',{'class':'com.sun.javafx.collections.ObservableListWrapper','reference':'../..'}); ET.SubElement(change,'changeLock').text='0'
   backing=ET.SubElement(wrap,'backingList')
   for child in value: backing.append(object_to_xml(child))
  else: xml_scalar(root,xmlname,kind,value)
 return root

def decompile_to_folder(data,parts_dir):
 root=parse_xdat(data); parts_dir=Path(parts_dir)
 for sub in ('windows','shortcuts'):
  folder=parts_dir/sub; folder.mkdir(parents=True,exist_ok=True)
  for stale in folder.glob('*.xml'):
   try: stale.unlink()
   except OSError: pass
 for item in root['windows']:
  name=item.get('name') or 'Window'
  safe=re.sub(r'[^A-Za-z0-9_.-]+','_',name)
  ET.ElementTree(object_to_xml(item)).write(parts_dir/'windows'/f'{safe}.xml',encoding='utf-8',xml_declaration=True)
 for item in root['shortcuts']:
  name=item.get('unk0') or 'Shortcut'
  safe=re.sub(r'[^A-Za-z0-9_.-]+','_',name)
  ET.ElementTree(object_to_xml(item)).write(parts_dir/'shortcuts'/f'{safe}.xml',encoding='utf-8',xml_declaration=True)
 meta={'schema':'ct0.XDAT','reserved':root.get('_reserved_0',1),'reserved_raw':root.get('__raw__',{}).get('_reserved_0',b'').hex(),'wndDefPos_raw':root.get('__raw__',{}).get('wndDefPos',b'').hex(),'tail':root.get('_tail',b'').hex(),'window_order':[x.get('name','') for x in root['windows']],'shortcut_order':[x.get('unk0','') for x in root['shortcuts']]}
 # Store only root metadata; objects are already represented as XML.
 (parts_dir/'__xdat_meta.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2),encoding='utf-8')
 return len(root['windows']),len(root['shortcuts'])

def encode_scalar(kind, value):
 if kind=='string': return put_string(value)
 if kind in ('int','bool') or kind.startswith('enum:'): return struct.pack('<i',int(value))
 if kind=='float': return struct.pack('<f',float(value))
 if kind=='compact': return put_compact(int(value))
 if kind=='color': return struct.pack('<I',int(value)&0xffffffff)
 return None

def merge_missing(dst, src):
 """Fill omitted XML properties from the matching original node; preserve byte-identical scalars."""
 if not src or dst.get('__type__')!=src.get('__type__'): return
 for name,kind in ops_for(dst['__type__']):
  if name in ('IF','IFNOT','END','_tail') or kind=='tail': continue
  if name not in dst or dst.get(name) is None:
   if name in src:
    dst[name]=src[name]
    if name in src.get('__raw__',{}): dst.setdefault('__raw__',{})[name]=src['__raw__'][name]
   continue
  if kind.startswith('list:'):
   da=dst.get(name,[]); sa=src.get(name,[])
   for i,(a,b) in enumerate(zip(da,sa)):
    if a.get('__type__')==b.get('__type__'):
     if b.get('__tag__'): a['__tag__']=b['__tag__']
     merge_missing(a,b)
   # Keep an original list's exact bytes when the edited XML still represents
   # the same records. If a user changes anything, the comparison fails and
   # the list is serialized from its fields instead.
   if name in src.get('__raw__',{}):
    try:
     encoded=bytearray(struct.pack('<i',len(da)))
     itemtype=kind[5:]
     for child in da:
      if itemtype=='poly':
       tag=child.get('__tag__',b'')
       encoded.extend(tag if tag else put_string(child['__type__'].split('.',1)[1]))
      write_node(child,encoded)
     if bytes(encoded)==src['__raw__'][name]:
      dst.setdefault('__raw__',{})[name]=src['__raw__'][name]
    except Exception:
     pass
  elif name in src.get('__raw__',{}):
   try:
    if encode_scalar(kind,dst[name])==src['__raw__'][name]: dst.setdefault('__raw__',{})[name]=src['__raw__'][name]
   except Exception: pass


def compile_from_folder(parts_dir,original_data=None):
 parts_dir=Path(parts_dir); windows=[]; shortcuts=[]
 for p in sorted((parts_dir/'windows').glob('*.xml'), key=lambda x:x.stem.casefold()) if (parts_dir/'windows').exists() else []:
  el=ET.parse(p).getroot(); obj=parse_xml_object(el)
  if obj['__type__']!='ct0.Window': raise ValueError(f'{p.name}: se esperaba ct0.Window')
  windows.append(obj)
 for p in sorted((parts_dir/'shortcuts').glob('*.xml'), key=lambda x:x.stem.casefold()) if (parts_dir/'shortcuts').exists() else []:
  el=ET.parse(p).getroot(); obj=parse_xml_object(el)
  if obj['__type__']!='ct0.Shortcut': raise ValueError(f'{p.name}: se esperaba ct0.Shortcut')
  shortcuts.append(obj)
 if not windows: raise ValueError('No hay archivos XML de ventanas en xdat por partes/windows')
 window_names=[x.get('name') for x in windows]
 shortcut_names=[x.get('unk0') for x in shortcuts]
 if len(window_names)!=len(set(window_names)): raise ValueError('Hay nombres de ventanas duplicados. Corrige los XML antes de compilar.')
 if len(shortcut_names)!=len(set(shortcut_names)): raise ValueError('Hay nombres de accesos directos duplicados. Corrige los XML antes de compilar.')
 meta_path=parts_dir/'__xdat_meta.json'
 meta={}
 orig=parse_xdat(original_data) if original_data else None
 if orig:
  meta={'reserved':orig.get('_reserved_0',1),'reserved_raw':orig.get('__raw__',{}).get('_reserved_0',b'').hex(),'wndDefPos_raw':orig.get('__raw__',{}).get('wndDefPos',b'').hex(),'tail':orig.get('_tail',b'').hex(),'window_order':[x.get('name','') for x in orig.get('windows',[])],'shortcut_order':[x.get('unk0','') for x in orig.get('shortcuts',[])]}
 if orig:
  originals={x.get('name'):x for x in orig.get('windows',[])}
  xml_windows={x.get('name'):x for x in windows}
  for x in windows:
   if x.get('name') in originals: merge_missing(x,originals[x.get('name')])
  # Keep the original binary's record order; append newly added windows afterwards.
  ordered=[xml_windows[x.get('name')] for x in orig.get('windows',[]) if x.get('name') in xml_windows]
  ordered_names={x.get('name') for x in ordered}
  windows=ordered+[x for x in windows if x.get('name') not in ordered_names]
  original_shortcuts={x.get('unk0'):x for x in orig.get('shortcuts',[])}
  xml_shortcuts={x.get('unk0'):x for x in shortcuts}
  for x in shortcuts:
   if x.get('unk0') in original_shortcuts: merge_missing(x,original_shortcuts[x.get('unk0')])
  ordered_s=[xml_shortcuts[x.get('unk0')] for x in orig.get('shortcuts',[]) if x.get('unk0') in xml_shortcuts]
  ordered_s_names={x.get('unk0') for x in ordered_s}
  shortcuts=ordered_s+[x for x in shortcuts if x.get('unk0') not in ordered_s_names]
 if meta_path.exists(): meta=json.loads(meta_path.read_text(encoding='utf-8'))
 elif orig:
  meta={'reserved':orig.get('_reserved_0',1),'reserved_raw':orig.get('__raw__',{}).get('_reserved_0',b'').hex(),'wndDefPos_raw':orig.get('__raw__',{}).get('wndDefPos',b'').hex(),'tail':orig.get('_tail',b'').hex()}
 if not orig and meta.get('window_order'):
  byname={x.get('name'):x for x in windows}; ordered=[byname[n] for n in meta['window_order'] if n in byname]; seen={x.get('name') for x in ordered}; windows=ordered+[x for x in windows if x.get('name') not in seen]
 if not orig and meta.get('shortcut_order'):
  byname={x.get('unk0'):x for x in shortcuts}; ordered=[byname[n] for n in meta['shortcut_order'] if n in byname]; seen={x.get('unk0') for x in ordered}; shortcuts=ordered+[x for x in shortcuts if x.get('unk0') not in seen]
 root={'__type__':'ct0.XDAT','__raw__':{},'windows':windows,'shortcuts':shortcuts,'_reserved_0':meta.get('reserved',1),'wndDefPos':[],'_tail':bytes.fromhex(meta.get('tail',''))}
 if meta.get('reserved_raw'): root['__raw__']['_reserved_0']=bytes.fromhex(meta['reserved_raw'])
 if meta.get('wndDefPos_raw'): root['__raw__']['wndDefPos']=bytes.fromhex(meta['wndDefPos_raw'])
 out=bytearray(); write_node(root,out)
 # Verify by parsing the output again and requiring exact roundtrip.
 check=parse_xdat(bytes(out)); verify=bytearray(); write_node(check,verify)
 if bytes(verify)!=bytes(out): raise ValueError('La verificación interna del XDAT compilado falló')
 return bytes(out),len(windows),len(shortcuts)
