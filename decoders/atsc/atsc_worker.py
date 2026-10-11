# SPDX-License-Identifier: GPL-3.0-or-later
# M.R.S. ATSC 1.0 worker. Uses GNU Radio's public ATSC receive blocks.
import argparse, json, math, os, sys, threading, time, subprocess, ctypes
runtime = os.path.dirname(sys.executable)
if os.name == 'nt':
    # Convert child DLL-loader failures into actionable setup errors, not modal dialogs.
    ctypes.windll.kernel32.SetErrorMode(0x8003)
_dll = os.add_dll_directory(os.path.join(runtime, 'Library', 'bin')) if os.name == 'nt' else None
from gnuradio import gr, blocks, filter, analog, dtv, network

def report(**fields):
    print(json.dumps(fields), flush=True)

class Receiver(gr.top_block):
    def __init__(self, rate, offset, output, raw_file=None, udp_port=0):
        super().__init__('MRS ATSC 1.0')
        if rate < 8000000 or rate > 20000000 or abs(offset) + 3000000 > rate / 2:
            raise ValueError('ATSC requires a complete 6 MHz channel inside an 8-20 MS/s capture.')
        self.source = blocks.file_source(1, raw_file, False) if raw_file else blocks.file_descriptor_source(1, 0, False)
        self.u8 = blocks.uchar_to_float()
        self.bias = blocks.add_const_ff(-127.5)
        self.scale = blocks.multiply_const_ff(1/128)
        self.split = blocks.deinterleave(gr.sizeof_float)
        self.iq = blocks.float_to_complex()
        decimation = 2 if rate >= 16000000 else 1
        taps = filter.firdes.low_pass(1, rate, 2900000, 200000)
        self.channel = filter.freq_xlating_fft_filter_ccc(decimation, taps, offset, rate)
        selected_rate = rate / decimation
        self.rx_filter = dtv.atsc_rx_filter(selected_rate, 1.5)
        output_rate = (4500000 / 286 * 684) * 1.5
        self.pll = dtv.atsc_fpll(output_rate)
        self.dc = filter.dc_blocker_ff(4096)
        self.agc = analog.agc_ff(1e-5, 4.0)
        self.sync = dtv.atsc_sync(output_rate)
        self.fields = dtv.atsc_fs_checker()
        self.equalizer = dtv.atsc_equalizer()
        self.viterbi = dtv.atsc_viterbi_decoder()
        self.deinterleaver = dtv.atsc_deinterleaver()
        self.rs = dtv.atsc_rs_decoder()
        self.derandomizer = dtv.atsc_derandomizer()
        self.depad = dtv.atsc_depad()
        self.output = blocks.file_sink(1, output, False)
        self.output.set_unbuffered(True)
        self.connect(self.source, self.u8, self.bias, self.scale, self.split)
        self.connect((self.split,0),(self.iq,0));self.connect((self.split,1),(self.iq,1))
        self.connect(self.iq, self.channel,
                     self.rx_filter, self.pll, self.dc, self.agc, self.sync, self.fields)
        stages = [self.fields, self.equalizer, self.viterbi, self.deinterleaver, self.rs, self.derandomizer]
        for a,b in zip(stages, stages[1:]):
            self.connect((a,0),(b,0)); self.connect((a,1),(b,1))
        self.connect((self.derandomizer,0),self.depad,self.output)
        if udp_port:
            self.udp = network.udp_sink(1,1,"127.0.0.1",udp_port,0,1316,False)
            self.connect(self.depad,self.udp)
    def stats(self, done=False):
        report(kind='status', packets=int(self.rs.num_packets()), bad_packets=int(self.rs.num_bad_packets()),
               corrected=int(self.rs.num_errors_corrected()), done=done)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--probe',action='store_true');parser.add_argument('--rate',type=int)
    parser.add_argument('--offset',type=float,default=0);parser.add_argument('--output');parser.add_argument('--raw-file');parser.add_argument('--udp-port',type=int,default=0)
    args=parser.parse_args()
    if args.probe:
        for filename in ('glib-2.0-0.dll', 'gmodule-2.0-0.dll'):
            if not os.path.isfile(os.path.join(runtime, 'Library', 'bin', filename)):
                raise RuntimeError('Incomplete TV runtime: missing '+filename+'. Run Set up TV decoder to repair it.')
        for filename, arguments in (('gdk-pixbuf-query-loaders.exe', []), ('ffprobe.exe', ['-version']), ('ffplay.exe', ['-version'])):
            result=subprocess.run([os.path.join(runtime, 'Library', 'bin', filename), *arguments],
                capture_output=True, timeout=10, creationflags=subprocess.CREATE_NO_WINDOW if os.name=='nt' else 0)
            if result.returncode:
                raise RuntimeError('TV runtime check failed for '+filename+' (exit '+str(result.returncode)+'). Run setup again.')
        report(kind='ready',version=gr.version(),standard='ATSC 1.0');return
    if os.name=='nt':
        import msvcrt
        msvcrt.setmode(0,os.O_BINARY)
    rx=Receiver(args.rate,args.offset,args.output,args.raw_file,args.udp_port)
    done=threading.Event()
    def telemetry():
        while not done.wait(1): rx.stats()
    threading.Thread(target=telemetry,daemon=True).start()
    report(kind='started',rate=args.rate,offset=args.offset)
    try: rx.run()
    finally: done.set();rx.stats(True)
if __name__=='__main__':
    try: main()
    except Exception as exc:
        report(kind='error',message=str(exc));sys.exit(1)
